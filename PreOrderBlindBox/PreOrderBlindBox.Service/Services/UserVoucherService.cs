﻿using AutoMapper;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.IRepositories;
using PreOrderBlindBox.Data.Repositories;
using PreOrderBlindBox.Data.UnitOfWork;
using PreOrderBlindBox.Services.DTO.RequestDTO.UserVoucherModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.UserVouchersModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.VoucherCampaignModel;
using PreOrderBlindBox.Services.IServices;
using PreOrderBlindBox.Services.Utils;

namespace PreOrderBlindBox.Service.Services
{
	public class UserVoucherService : IUserVoucherService
	{
		private readonly ICurrentUserService _currentUserService;
		private readonly IVoucherCampaignService _voucherCampaignService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserVoucherRepository _userVoucherRepository;
		private readonly IVoucherCampaignRepository _voucherCampaignRepository;
		private readonly IMapper _mapper;

		public UserVoucherService(ICurrentUserService currentUserService, IUserVoucherRepository userVoucherRepository
			, IVoucherCampaignService voucherCampaignService, IUnitOfWork unitOfWork, IVoucherCampaignRepository voucherCampaignRepository
			, IMapper mapper)
		{
			_currentUserService = currentUserService;
			_voucherCampaignService = voucherCampaignService;
			_unitOfWork = unitOfWork;
			_userVoucherRepository = userVoucherRepository;
			_voucherCampaignRepository = voucherCampaignRepository;
			_mapper = mapper;
		}
		public async Task<ResponseCreateUserVoucher> CreateUserVoucherAsync(RequestCreateUserVoucher userVoucher)
		{
			int userId = _currentUserService.GetUserId();

			if (userVoucher == null)
			{
				throw new ArgumentNullException("User voucher cannot be null");
			}

			VoucherCampaign voucherCampaign = await _voucherCampaignService.GetVoucherCampaignEntityById(userVoucher.VoucherCampaignId);
			if (voucherCampaign.Quantity - voucherCampaign.TakenQuantity <= 0)
			{
				throw new Exception("No more voucher to get");
			}
			if (DateTime.Now > voucherCampaign.EndDate)
			{
				throw new Exception("The pick up time has ended");
			}

			// Kiểm tra user đã lấy voucher chưa
			var checkUserVoucher = await _userVoucherRepository.GetUserVoucherByUserIdAndVoucherCampaignId(userId, voucherCampaign.VoucherCampaignId);
			if (checkUserVoucher != null)
			{
				throw new Exception("User has received voucher");
			}

			await _unitOfWork.BeginTransactionAsync();
			try
			{
				// Tính số lượng voucher mà user có thể thật sự lấy
				int quantity = voucherCampaign.Quantity - voucherCampaign.TakenQuantity > voucherCampaign.MaximumUserCanGet
								? voucherCampaign.MaximumUserCanGet : voucherCampaign.Quantity - voucherCampaign.TakenQuantity;

				// Cập nhật lại voucher campaign
				voucherCampaign.TakenQuantity += quantity;
				await _voucherCampaignRepository.UpdateAsync(voucherCampaign);

				UserVoucher newUserVoucher = new UserVoucher()
				{
					UserId = userId,
					VoucherCampaignId = voucherCampaign.VoucherCampaignId,
					Quantity = quantity,
					UsedQuantity = 0,
					CreatedDate = DateTime.Now,
				};
				await _userVoucherRepository.InsertAsync(newUserVoucher);
				await _unitOfWork.SaveChanges();
				await _unitOfWork.CommitTransactionAsync();

				return _mapper.Map<ResponseCreateUserVoucher>(newUserVoucher);
			}
			catch (Exception ex)
			{
				await _unitOfWork.RollbackTransactionAsync();
				throw;
			}

		}

		public async Task<int> UpdateUserVoucherAsync(RequestUpdateUserVoucher updateUserVoucher)
		{
			int userId = _currentUserService.GetUserId();

			if (updateUserVoucher == null)
			{
				throw new ArgumentNullException("Update user voucher cannot be null");
			}

			VoucherCampaign voucherCampaign = await _voucherCampaignService.GetVoucherCampaignEntityById(updateUserVoucher.VoucherCampaignId);
			var userVoucher = await _userVoucherRepository.GetUserVoucherByUserIdAndVoucherCampaignId(userId, updateUserVoucher.VoucherCampaignId);
			if (userVoucher == null)
			{
				throw new Exception("User does not have voucher");
			}
			if (DateTime.Now >= voucherCampaign.ExpiredDate)
			{
				throw new Exception("Voucher has expired");
			}
			if (userVoucher.UsedQuantity >= userVoucher.Quantity)
			{
				throw new Exception("No more vouchers to use");
			}

			userVoucher.UsedQuantity += 1;
			await _userVoucherRepository.UpdateAsync(userVoucher);
			return await _unitOfWork.SaveChanges();
		}
	}
}
