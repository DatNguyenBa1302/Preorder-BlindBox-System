﻿using Azure;
using PreOrderBlindBox.Data.Commons;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.Enum;
using PreOrderBlindBox.Data.IRepositories;
using PreOrderBlindBox.Data.Repositories;
using PreOrderBlindBox.Data.UnitOfWork;
using PreOrderBlindBox.Service.Services;
using PreOrderBlindBox.Services.DTO.RequestDTO.CartRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.NotificationRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.OrderRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.TransactionRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.UserVoucherModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.CartResponseModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.OrderResponseModel;
using PreOrderBlindBox.Services.IServices;
using PreOrderBlindBox.Services.Mappers.CartMapper;
using PreOrderBlindBox.Services.Mappers.OrderDetailMapper;
using PreOrderBlindBox.Services.Mappers.OrderMapper;
using PreOrderBlindBox.Services.Mappers.TempCampaignBulkOrderMapper;
using PreOrderBlindBox.Services.Utils;

namespace PreOrderBlindBox.Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderDetailService _orderDetailService;
        private readonly IUserRepository _userRepository;
        private readonly ICartService _cartService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserVoucherService _userVoucherService;
        private readonly ITransactionService _transactionService;
        private readonly ITempCampaignBulkOrderService _tempCampaignBulkOrderService;
        private readonly ITempCampaignBulkOrderDetailService _tempCampaignBulkOrderDetailService;
        private readonly IPreorderCampaignRepository _preorderCampaignRepository;

        public OrderService(
            IOrderRepository orderRepository, ICartService cartService,
            IUnitOfWork unitOfWork, IUserRepository userRepository,
            INotificationService notificationService, IOrderDetailService orderDetailService,
            ICurrentUserService currentUserService,
            IUserVoucherService userVoucherService,
            ITransactionService transactionService,
            ITempCampaignBulkOrderService tempCampaignBulkOrderService,
            ITempCampaignBulkOrderDetailService tempCampaignBulkOrderDetailService,
        IPreorderCampaignRepository preorderCampaignRepository
            )
        {
            _orderRepository = orderRepository;
            _cartService = cartService;
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _orderDetailService = orderDetailService;
            _currentUserService = currentUserService;
            _userVoucherService = userVoucherService;
            _transactionService = transactionService;
            _tempCampaignBulkOrderService = tempCampaignBulkOrderService;
            _preorderCampaignRepository = preorderCampaignRepository;
            _tempCampaignBulkOrderDetailService = tempCampaignBulkOrderDetailService;
        }

        public async Task CreateOrder(RequestCreateOrder requestCreateOrder)
        {
            int customerId = _currentUserService.GetUserId();
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var customer = await _userRepository.GetByIdAsync(customerId);
                var staffs = (await _userRepository.GetAll(filter: x => x.Role.RoleName == "Staff", includes: x => x.Role)).ToList();
                var admin = (await _userRepository.GetAll(filter: x => x.Role.RoleName == "Admin", includes: x => x.Role)).FirstOrDefault();

                var notificationForCustomer = (new RequestCreateNotification()).NotificationForCustomer(customerId);
                List<ResponseCartWithVoucher> priceForCarts = await _cartService.IdentifyPriceForCartItem(customerId, requestCreateOrder.UserVoucherIdForPreorderCampaign, requestCreateOrder.RequestCreateCart);
                if (priceForCarts.Count == 0)
                    throw new Exception("The cart is empty");

                foreach (var item in priceForCarts)
                {
                    var requestAdminTransactionCreateModel = new RequestTransactionCreateModel()
                    {
                        
                        Money = 0,
                        WalletId = admin.WalletId,
                        Type = TypeOfTransactionEnum.Recharge,
                    };

                    var requestCustomerTransactionCreateModel = new RequestTransactionCreateModel()
                    {
                        
                        Money = 0,
                        WalletId = customer.WalletId,
                        Type = TypeOfTransactionEnum.Purchase,
                    };

                    int preorderCampaignId = (int)item.responseCarts.FirstOrDefault().PreorderCampaignId;
                    requestCreateOrder.DiscountMoney = item.DiscountMoney;
                    var preorderCampaign = await _preorderCampaignRepository.GetDetailPreorderCampaignById(preorderCampaignId);
                    var userVoucher = item.UserVoucher != null ? await _userVoucherService.GetUserVoucherById(item.UserVoucher.UserVoucherId) : null ;
                    requestCreateOrder.Amount = item.Total;

                    if (preorderCampaign.Type.Equals("BulkOrder"))
                    {
                        var tempCampaignBulkOrderEntity = requestCreateOrder.toTempCampaignBulkOrder(customerId) ;
                        
                        if(userVoucher != null) tempCampaignBulkOrderEntity.UserVoucherId = userVoucher.UserVoucherId;
                        tempCampaignBulkOrderEntity = await _tempCampaignBulkOrderService.CreateOrder(tempCampaignBulkOrderEntity);
                        await _unitOfWork.SaveChanges();
                        await _tempCampaignBulkOrderDetailService.CreateTempCampaignBulkOrderDetail(item.responseCarts, tempCampaignBulkOrderEntity.TempCampaignBulkOrderId);
                        requestAdminTransactionCreateModel.TempCampaignBulkOrderId = tempCampaignBulkOrderEntity.TempCampaignBulkOrderId;
                        requestCustomerTransactionCreateModel.TempCampaignBulkOrderId = tempCampaignBulkOrderEntity.TempCampaignBulkOrderId;
						requestAdminTransactionCreateModel.Description = $"The system has received a payment from the user {customer.FullName} for Order #{tempCampaignBulkOrderEntity.TempCampaignBulkOrderId}, and an amount of {item.Total} has been credited to the admin's wallet.";
                        requestCustomerTransactionCreateModel.Description = $"When the user makes a payment for Order #{tempCampaignBulkOrderEntity.TempCampaignBulkOrderId}, an amount of {item.Total} has been deducted from the user {customer.FullName}'s wallet.";
                    }
                    else
                    {
                        var orderEntity = requestCreateOrder.toOrderEntity(customerId);
						if (userVoucher != null) orderEntity.UserVoucherId = userVoucher.UserVoucherId;
						await _orderRepository.InsertAsync(orderEntity);
                        await _unitOfWork.SaveChanges();
                        await _orderDetailService.CreateOrderDetail(item.responseCarts, orderEntity.OrderId);
                        requestAdminTransactionCreateModel.OrderId = orderEntity.OrderId;
                        requestCustomerTransactionCreateModel.OrderId = orderEntity.OrderId;
						requestAdminTransactionCreateModel.Description = $"The system has received a payment from the user {customer.FullName} for Order #{orderEntity.OrderId}, and an amount of {item.Total} has been credited to the admin's wallet.";
						requestCustomerTransactionCreateModel.Description = $"When the user makes a payment for Order #{orderEntity.OrderId}, an amount of {item.Total} has been deducted from the user {customer.FullName}'s wallet.";
					}
                    if (userVoucher != null)
                        await _userVoucherService.UpdateUserVoucherAsync(new RequestUpdateUserVoucher() { VoucherCampaignId = (int)userVoucher.VoucherCampaignId });
                    preorderCampaign.PlacedOrderCount += item.responseCarts.Sum(x => x.Quantity);
                    requestAdminTransactionCreateModel.Money = item.Total;
                    requestCustomerTransactionCreateModel.Money = item.Total;
                    if (!await _transactionService.CreateTransaction(requestCustomerTransactionCreateModel))
                        throw new Exception("Not enough money in your wallet !");
                    await _transactionService.CreateTransaction(requestAdminTransactionCreateModel);
                    await _preorderCampaignRepository.UpdateAsync(preorderCampaign);
                    await _unitOfWork.SaveChanges();
                }
                if (requestCreateOrder.RequestCreateCart.PreorderCampaignId == null)
                    await _cartService.UpdateStatusOfCartByCustomerID(customerId);

                foreach (var staff in staffs)
                {
					var notificationForStaff = (new RequestCreateNotification()).NotificationForStaff(customer.FullName, staff.UserId);
					await _notificationService.CreatNotification(notificationForStaff);
				}
				await _notificationService.CreatNotification(notificationForCustomer);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;

            }
        }

        public async Task<Pagination<ResponseOrder>> GetAllOrder(PaginationParameter page, string? searchKeyWords, string orderBy)
        {
            List<Order> orders = new List<Order>();

            if (orderBy.Equals("increase"))
            {
                orders = await _orderRepository.GetAll(filter: x => (x.ReceiverName.Contains(searchKeyWords) || x.ReceiverAddress.Contains(searchKeyWords) || String.IsNullOrEmpty(searchKeyWords))
                                                        , pagination: page, includes: x => x.OrderDetails, orderBy: x => x.OrderBy(x => x.CreatedDate));
            }
            else if (orderBy.Equals("decrease"))
            {
                orders = await _orderRepository.GetAll(filter: x => (x.ReceiverName.Contains(searchKeyWords) || x.ReceiverAddress.Contains(searchKeyWords) || String.IsNullOrEmpty(searchKeyWords))
                                                        , pagination: page, includes: x => x.OrderDetails, orderBy: x => x.OrderByDescending(x => x.CreatedDate));
            }

            var itemsOrderDetail = orders.Select(x => x.toOrderRespone()).ToList();
            var countItem = _orderRepository.Count(filter: x => (x.ReceiverName.Contains(searchKeyWords) || x.ReceiverAddress.Contains(searchKeyWords) || String.IsNullOrEmpty(searchKeyWords)));
            var result = new Pagination<ResponseOrder>(itemsOrderDetail, countItem, page.PageIndex, page.PageSize);
            return result;
        }

        public async Task<ResponseOrder> GetOrderByIdForStaff(int id)
        {
            var orderById = await _orderRepository.GetByIdAsync(id);
            var orderByIdResponse = orderById.toOrderRespone();
            return orderByIdResponse;

        }

        public async Task<ResponseOrder> GetOrderByIdForCustomer(int id)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var user = await _userRepository.GetUserById(userId);
                var orderById = await _orderRepository.GetByIdAsync(id);
                if (orderById == null)
                    return null;
                if (user?.Role.RoleName == "Customer" && orderById.CustomerId != userId)
                {
                    throw new Exception("You do not have permission to access this order");
                }
                var orderByIdResponse = orderById.toOrderRespone();
                return orderByIdResponse;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<ResponseOrder> UpdateStatusOfOrder(int orderId, RequestUpdateOrder requestUpdateOrder)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    throw new ArgumentException("Order not found");
                }
                order.Status = requestUpdateOrder.Status;
                await _orderRepository.UpdateAsync(order);
                await _unitOfWork.SaveChanges();
                await _unitOfWork.CommitTransactionAsync();
                return order.toOrderRespone();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<Pagination<ResponseOrder>> OrderHistory(PaginationParameter pagination)
        {
            int customerId = _currentUserService.GetUserId();
            var orders = await _orderRepository.GetAll(
                filter: x => x.CustomerId == customerId,
                includes: x => x.OrderDetails,
                pagination: pagination,
                orderBy: x => x.OrderBy(y => y.Status.Equals("Delivered") ? 1 :
                                            y.Status.Equals("Delivering") ? 2 :
                                            y.Status.Equals("Processing") ? 3 :
                                            y.Status.Equals("Placed") ? 4 : 5
                ).ThenByDescending(x => x.CreatedDate)
                );
            var itemsOrderDetail = orders.Select(x => x.toOrderRespone()).ToList();
            var countItem = _orderRepository.Count();
            var result = new Pagination<ResponseOrder>(itemsOrderDetail, countItem, pagination.PageIndex, pagination.PageSize);
            return result;
        }
    }
}
