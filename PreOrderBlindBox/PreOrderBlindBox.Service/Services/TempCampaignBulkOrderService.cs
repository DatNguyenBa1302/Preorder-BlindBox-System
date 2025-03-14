﻿using PreOrderBlindBox.Data.Commons;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.Enum;
using PreOrderBlindBox.Data.IRepositories;
using PreOrderBlindBox.Data.Repositories;
using PreOrderBlindBox.Data.UnitOfWork;
using PreOrderBlindBox.Service.Services;
using PreOrderBlindBox.Services.DTO.RequestDTO.CartRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.OrderRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.TempCampaignBulkOrderModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.TransactionRequestModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.OrderResponseModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.TempCampaignBulkOrderModel;
using PreOrderBlindBox.Services.IServices;
using PreOrderBlindBox.Services.Mappers.OrderMapper;
using PreOrderBlindBox.Services.Mappers.TempCampaignBulkOrderMapper;
using PreOrderBlindBox.Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOrderBlindBox.Services.Services
{
    public class TempCampaignBulkOrderService : ITempCampaignBulkOrderService
    {
        private readonly ITempCampaignBulkOrderRepository _tempCampaignBulkOrderRepository;
        private readonly ITempCampaignBulkOrderDetailRepository _tempCampaignBulkOrderDetailRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderDetailService _orderDetailService;
        private readonly IUserVoucherService _userVoucherService;
        private readonly IPreorderCampaignService _preorderCampaignService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;
        private readonly ITransactionService _transactionService;

        public TempCampaignBulkOrderService(
            ITempCampaignBulkOrderRepository tempCampaignBulkOrderRepository,
            ITempCampaignBulkOrderDetailRepository tempCampaignBulkOrderDetailRepository,
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IOrderDetailService orderDetailService,
            IUserVoucherService userVoucherService,
            IPreorderCampaignService preorderCampaignService,
            ICurrentUserService currentUserService,
            IUserRepository userRepository,
            ITransactionService transactionService
            )
        {
            _tempCampaignBulkOrderRepository = tempCampaignBulkOrderRepository;
            _tempCampaignBulkOrderDetailRepository = tempCampaignBulkOrderDetailRepository;
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _orderDetailService = orderDetailService;
            _userVoucherService = userVoucherService;
            _preorderCampaignService = preorderCampaignService;
            _currentUserService = currentUserService;
            _userRepository = userRepository;
            _transactionService = transactionService;
        }

        public async Task<bool> AcceptTempOrder(int preorderCampaignId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
				var admin = (await _userRepository.GetAll(filter: x => x.Role.RoleName == "Admin", includes: x => x.Role)).FirstOrDefault();
				var temCampaignBulkOrderByPreorderCampaignId = await _tempCampaignBulkOrderRepository.GetAll(filter: x => x.TempCampaignBulkOrderDetails.Any(d => d.PreorderCampaignId == preorderCampaignId), includes: x => x.TempCampaignBulkOrderDetails);
                var preorderCampaign = await _preorderCampaignService.GetPreorderCampaignAsyncById(preorderCampaignId);
                if (preorderCampaign == null)
                    throw new Exception("Preorder campaign is not valid");
                if(temCampaignBulkOrderByPreorderCampaignId.Any(x=>x.Status != "Waiting"))
                    throw new Exception("Order has been accepted or rejected");
                var endPriceOfCampaign = (await _preorderCampaignService.GetPreorderCampaignBySlugAsync(preorderCampaign.Slug)).PriceAtTime;
                foreach (var item in temCampaignBulkOrderByPreorderCampaignId)
                {
                    var customer = await _userRepository.GetByIdAsync(item.CustomerId);
					var requestAdminTransactionCreateModel = new RequestTransactionCreateModel()
					{
						Description = "Purchase",
						Money = 0,
						WalletId = admin.WalletId,
						Type = TypeOfTransactionEnum.Purchase,
					};

					var requestCustomerTransactionCreateModel = new RequestTransactionCreateModel()
					{
						Money = 0,
						WalletId = customer.WalletId,
						Type = TypeOfTransactionEnum.Refund,
					};

					var temCampaignBulkOrderDetailList = await _tempCampaignBulkOrderDetailRepository.GetAll(filter: x => x.TempCampaignBulkOrderId == item.TempCampaignBulkOrderId);
                    decimal totalTempPreorderDetail = temCampaignBulkOrderDetailList.Sum(x => x.Quantity) * endPriceOfCampaign;
                    var userVoucher = await _userVoucherService.GetUserVoucherById((int)item.UserVoucherId);
                    decimal discountMoney = (decimal) (totalTempPreorderDetail * (userVoucher.PercentDiscount / 100) > userVoucher.MaximumMoneyDiscount ? userVoucher.MaximumMoneyDiscount : totalTempPreorderDetail * (userVoucher.PercentDiscount / 100));
					var orderEntity = new Order()
                    {
                        Amount = totalTempPreorderDetail - discountMoney,
                        DiscountMoney = discountMoney,
                        CustomerId = item.CustomerId,
                        ReceiverName = item.ReceiverName,
                        ReceiverAddress = item.ReceiverAddress,
                        ReceiverPhone = item.ReceiverPhone,
                        UserVoucherId = item.UserVoucherId,
                        Status = "Placed",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    };
                    foreach (var itemTempOrderDetail in temCampaignBulkOrderDetailList)
                    {
                        itemTempOrderDetail.UnitEndCampaignPrice = endPriceOfCampaign;
                        await _tempCampaignBulkOrderDetailRepository.UpdateAsync(itemTempOrderDetail);
                    }
                    item.Status = "Approve";
                    await _tempCampaignBulkOrderRepository.UpdateAsync(item);
                    await _orderRepository.InsertAsync(orderEntity);
                    await _unitOfWork.SaveChanges();
                    if(item.Amount -orderEntity.Amount  > 0)
                    {
						requestAdminTransactionCreateModel.OrderId = orderEntity.OrderId;
						requestCustomerTransactionCreateModel.OrderId = orderEntity.OrderId;
						requestAdminTransactionCreateModel.Money = item.Amount - orderEntity.Amount;
						requestCustomerTransactionCreateModel.Money = item.Amount - orderEntity.Amount;
						requestCustomerTransactionCreateModel.Description = $"You have received a refund of {requestAdminTransactionCreateModel.Money} as the campaign {preorderCampaign.BlindBox.Name} has been accepted. The amount has been credited to your wallet";
                        requestAdminTransactionCreateModel.Description = $"A refund of {requestAdminTransactionCreateModel.Money} has been processed for the customer {customer.FullName} from the campaign {preorderCampaign.BlindBox.Name}.";
						if (!await _transactionService.CreateTransaction(requestAdminTransactionCreateModel))
							throw new Exception("Not enough money in your wallet !");
						await _transactionService.CreateTransaction(requestCustomerTransactionCreateModel);
					}
					await _orderDetailService.CreateTempOrderDetailToOrderDetail(temCampaignBulkOrderDetailList, orderEntity.OrderId, endPriceOfCampaign);
                }
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

        }

        public async Task<TempCampaignBulkOrder> CreateOrder(TempCampaignBulkOrder tempCampaignBulkOrder)
        {
            await _tempCampaignBulkOrderRepository.InsertAsync(tempCampaignBulkOrder);
            await _unitOfWork.SaveChanges();
            return tempCampaignBulkOrder;
        }

        public async Task<Pagination<ResponseTempCampaignBulkOrder>> GetAllTempOrder(PaginationParameter page, string? searchKeyWords, string orderBy)
        {
            List<TempCampaignBulkOrder> tempCampaignBulkOrder = new List<TempCampaignBulkOrder>();

            if (orderBy.Equals("increase"))
            {
                tempCampaignBulkOrder = await _tempCampaignBulkOrderRepository.GetAll(filter: x => (x.ReceiverName.Contains(searchKeyWords) || x.ReceiverAddress.Contains(searchKeyWords) || String.IsNullOrEmpty(searchKeyWords))
                                                        , pagination: page, includes: x => x.TempCampaignBulkOrderDetails, orderBy: x => x.OrderBy(x => x.CreatedDate));
            }
            else if (orderBy.Equals("decrease"))
            {
                tempCampaignBulkOrder = await _tempCampaignBulkOrderRepository.GetAll(filter: x => (x.ReceiverName.Contains(searchKeyWords) || x.ReceiverAddress.Contains(searchKeyWords) || String.IsNullOrEmpty(searchKeyWords))
                                                        , pagination: page, includes: x => x.TempCampaignBulkOrderDetails, orderBy: x => x.OrderByDescending(x => x.CreatedDate));
            }

            var itemsOrderDetail = tempCampaignBulkOrder.Select(x => x.toTempCampaignBulkOrderRespone()).ToList();
            var countItem = _tempCampaignBulkOrderRepository.Count(filter: x => (x.ReceiverName.Contains(searchKeyWords) || x.ReceiverAddress.Contains(searchKeyWords) || String.IsNullOrEmpty(searchKeyWords)));
            var result = new Pagination<ResponseTempCampaignBulkOrder>(itemsOrderDetail, countItem, page.PageIndex, page.PageSize);
            return result;
        }

        public async Task<ResponseTempCampaignBulkOrder> GetTempOrderByIdForCustomer(int id)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var user = await _userRepository.GetUserById(userId);
                var tempOrderById = await _tempCampaignBulkOrderRepository.GetByIdAsync(id);
                if (tempOrderById == null)
                    return null;
                if (user?.Role.RoleName == "Customer" && tempOrderById.CustomerId != userId)
                {
                    throw new Exception("You do not have permission to access this order");
                }
                var orderByIdResponse = tempOrderById.toTempCampaignBulkOrderRespone();
                return orderByIdResponse;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
            
        public async Task<bool> RejectTempOrder(int preorderCampaignId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
				var admin = (await _userRepository.GetAll(filter: x => x.Role.RoleName == "Admin", includes: x => x.Role)).FirstOrDefault();
				var temCampaignBulkOrderByPreorderCampaignId = await _tempCampaignBulkOrderRepository.GetAll(filter: x => x.TempCampaignBulkOrderDetails.Any(d => d.PreorderCampaignId == preorderCampaignId), includes: x => x.TempCampaignBulkOrderDetails);
                var preorderCampaign = await _preorderCampaignService.GetPreorderCampaignAsyncById(preorderCampaignId);
                if (preorderCampaign == null)
                    throw new Exception("Preorder campaign is not valid");
                if (temCampaignBulkOrderByPreorderCampaignId.Any(x => x.Status != "Waiting"))
                    throw new Exception("Order has been accepted or rejected");
                foreach (var item in temCampaignBulkOrderByPreorderCampaignId)
                {
					var customer = await _userRepository.GetByIdAsync(item.CustomerId);
					var requestAdminTransactionCreateModel = new RequestTransactionCreateModel()
					{
						Money = 0,
						WalletId = admin.WalletId,
						Type = TypeOfTransactionEnum.Purchase,
					};

					var requestCustomerTransactionCreateModel = new RequestTransactionCreateModel()
					{
						Money = 0,
						WalletId = customer.WalletId,
						Type = TypeOfTransactionEnum.Refund,
					};
					var temCampaignBulkOrderDetailList = await _tempCampaignBulkOrderDetailRepository.GetAll(filter: x => x.TempCampaignBulkOrderId == item.TempCampaignBulkOrderId);
                    item.Status = "Reject";
                    await _tempCampaignBulkOrderRepository.UpdateAsync(item);
                    foreach (var itemTempOrderDetail in temCampaignBulkOrderDetailList)
                    {
                        itemTempOrderDetail.UnitEndCampaignPrice = itemTempOrderDetail.UnitPriceAtTime;
                        await _tempCampaignBulkOrderDetailRepository.UpdateAsync(itemTempOrderDetail);
                    }
					requestAdminTransactionCreateModel.TempCampaignBulkOrderId = item.TempCampaignBulkOrderId;
					requestCustomerTransactionCreateModel.TempCampaignBulkOrderId = item.TempCampaignBulkOrderId;
					requestAdminTransactionCreateModel.Money = item.Amount;
					requestCustomerTransactionCreateModel.Money = item.Amount;
                    requestAdminTransactionCreateModel.Description = $"A refund of {item.Amount} has been processed for the customer {customer.FullName} due to the rejection of the {preorderCampaign.BlindBox.Name} campaign.";
                    requestCustomerTransactionCreateModel.Description = $"You have received a refund of {item.Amount} as the campaign {preorderCampaign.BlindBox.Name} has been rejected. The amount has been credited to your wallet.";
					if (!await _transactionService.CreateTransaction(requestAdminTransactionCreateModel))
						throw new Exception("Not enough money in your wallet !");
					await _transactionService.CreateTransaction(requestCustomerTransactionCreateModel);
					await _unitOfWork.SaveChanges();
                }
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<Pagination<ResponseTempCampaignBulkOrder>> TempOrderHistory(PaginationParameter pagination)
        {
            int customerId = _currentUserService.GetUserId();
            var tempCampaignBulkOrder = await _tempCampaignBulkOrderRepository.GetAll(
                filter: x => x.CustomerId == customerId,
                includes: x => x.TempCampaignBulkOrderDetails,
                pagination: pagination,
                orderBy: x => x.OrderBy(y => y.Status.Equals("Approve") ? 1 :
                                            y.Status.Equals("Waiting") ? 2 :
                                            y.Status.Equals("Reject") ? 3 : 4
                ).ThenByDescending(x => x.CreatedDate)
                );
            var itemstempCampaignBulkOrderDetail = tempCampaignBulkOrder.Select(x => x.toTempCampaignBulkOrderRespone()).ToList();
            var countItem = _tempCampaignBulkOrderRepository.Count();
            var result = new Pagination<ResponseTempCampaignBulkOrder>(itemstempCampaignBulkOrderDetail, countItem, pagination.PageIndex, pagination.PageSize);
            return result;
        }
    }
}
