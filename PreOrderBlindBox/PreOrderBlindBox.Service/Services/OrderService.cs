﻿using Azure;
using PreOrderBlindBox.Data.Commons;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.IRepositories;
using PreOrderBlindBox.Data.UnitOfWork;
using PreOrderBlindBox.Services.DTO.RequestDTO.CartRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.NotificationRequestModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.OrderRequestModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.CartResponseModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.OrderResponseModel;
using PreOrderBlindBox.Services.IServices;
using PreOrderBlindBox.Services.Mappers.OrderDetailMapper;
using PreOrderBlindBox.Services.Mappers.OrderMapper;
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

		public OrderService(
			IOrderRepository orderRepository, ICartService cartService,
			IUnitOfWork unitOfWork, IUserRepository userRepository,
			INotificationService notificationService, IOrderDetailService orderDetailService,
			ICurrentUserService currentUserService,
			IUserVoucherService userVoucherService
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
		}

		public async Task CreateOrder(RequestCreateOrder requestCreateOrder, RequestCreateCart? requestCreateCart)
		{
			int customerId = _currentUserService.GetUserId();
			await _unitOfWork.BeginTransactionAsync();
			try
			{
				var customer = await _userRepository.GetByIdAsync(customerId);
				var staff = (await _userRepository.GetAll(filter: x => x.Role.RoleName == "Staff", includes: x => x.Role)).FirstOrDefault();
				var notificationForCustomer = (new RequestCreateNotification()).NotificationForCustomer(customerId);
				var notificationForStaff = (new RequestCreateNotification()).NotificationForStaff(customer.FullName, staff.UserId);
				List<ResponseCart> priceForCarts = await _cartService.IdentifyPriceForCartItem(customerId, requestCreateCart);
				if (priceForCarts.Count == 0)
					throw new Exception("The cart is empty");
				List<ResponseCart> cartInSameCampaign = new List<ResponseCart>();
				int preorderCampaignId = (int)priceForCarts[0].PreorderCampaignId;
				cartInSameCampaign.Add(priceForCarts[0]);
				for (int i = 1; i < priceForCarts.Count; i++)
				{
					if (priceForCarts[i].PreorderCampaignId != preorderCampaignId)
					{
						if (requestCreateOrder.UserVoucherId != null)
						{
							var userVoucher = await _userVoucherService.GetUserVoucherById((int)requestCreateOrder.UserVoucherId);
							var amountAfterUsingVoucher = cartInSameCampaign.Sum(x => x.Amount) * (userVoucher.PercentDiscount / 100);
							if (amountAfterUsingVoucher > userVoucher.MaximumMoneyDiscount)
							{
								requestCreateOrder.Amount = userVoucher.MaximumMoneyDiscount;
							}
							else
							{
								requestCreateOrder.Amount = amountAfterUsingVoucher;
							}
						}
						else
						{
							requestCreateOrder.Amount = cartInSameCampaign.Sum(x => x.Amount);
						}

						var orderEntity = requestCreateOrder.toOrderEntity(customerId);
						await _orderRepository.InsertAsync(orderEntity);
						await _unitOfWork.SaveChanges();
						await _orderDetailService.CreateOrderDetail(cartInSameCampaign, orderEntity.OrderId);
						cartInSameCampaign = new List<ResponseCart> { priceForCarts[i] };
						preorderCampaignId = (int)priceForCarts[i].PreorderCampaignId;

					}
					else
					{
						cartInSameCampaign.Add(priceForCarts[i]);
					}

					if (i == priceForCarts.Count - 1)
					{
						if (requestCreateOrder.UserVoucherId != null)
						{
							var userVoucher = await _userVoucherService.GetUserVoucherById((int)requestCreateOrder.UserVoucherId);
							var amountAfterUsingVoucher = cartInSameCampaign.Sum(x => x.Amount) * (userVoucher.PercentDiscount / 100);
							if (amountAfterUsingVoucher > userVoucher.MaximumMoneyDiscount)
							{
								requestCreateOrder.Amount = userVoucher.MaximumMoneyDiscount;
							}
							else
							{
								requestCreateOrder.Amount = amountAfterUsingVoucher;
							}
						}
						else
						{
							requestCreateOrder.Amount = cartInSameCampaign.Sum(x => x.Amount);
						}

						var orderEntity = requestCreateOrder.toOrderEntity(customerId);
						await _orderRepository.InsertAsync(orderEntity);
						await _unitOfWork.SaveChanges();
						await _orderDetailService.CreateOrderDetail(cartInSameCampaign, orderEntity.OrderId);

					}
				}
				if (priceForCarts.ToList().Any(x => x.Price < 0))
				{
					throw new Exception($"The cart contains {priceForCarts[0].Quantity} item with an incorrect price");
				}

				await _cartService.UpdateStatusOfCartByCustomerID(customerId);
				await _notificationService.CreatNotification(notificationForStaff);
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
			var result = new Pagination<ResponseOrder>(itemsOrderDetail, itemsOrderDetail.Count, page.PageIndex, page.PageSize);
			return result;
		}

		public async Task<ResponseOrder> GetOrderById(int id)
		{
			var orderById = await _orderRepository.GetByIdAsync(id);
			var orderByIdResponse = orderById.toOrderRespone();

			return orderByIdResponse;
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
			var orders = await _orderRepository.GetAll(filter: x => x.CustomerId == customerId, includes: x => x.OrderDetails, pagination: pagination);
			var itemsOrderDetail = orders.Select(x => x.toOrderRespone()).ToList();
			var result = new Pagination<ResponseOrder>(itemsOrderDetail, itemsOrderDetail.Count, pagination.PageIndex, pagination.PageSize);
			return result;
		}
	}
}
