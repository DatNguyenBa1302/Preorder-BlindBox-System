﻿using AutoMapper;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.Enum;
using PreOrderBlindBox.Data.IRepositories;
using PreOrderBlindBox.Data.Repositories;
using PreOrderBlindBox.Data.UnitOfWork;
using PreOrderBlindBox.Services.DTO.RequestDTO.VoucherCampaignModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.VoucherCampaignModel;
using PreOrderBlindBox.Services.IServices;
using PreOrderBlindBox.Services.Utils;

namespace PreOrderBlindBox.Service.Services
{
	public class VoucherCampaignService : IVoucherCampaignService
	{
		private readonly IVoucherCampaignRepository _voucherCampaignRepository;
		private readonly IMapper _mapper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICurrentUserService _currentUserService;
		private readonly IUserVoucherRepository _userVoucherRepository;
		private readonly IUserRepository _userRepository;

		public VoucherCampaignService(IVoucherCampaignRepository voucherCampaignRepository, IMapper mapper, IUnitOfWork unitOfWork
			, ICurrentUserService currentUserService, IUserVoucherRepository userVoucherRepository, IUserRepository userRepository)
		{
			_voucherCampaignRepository = voucherCampaignRepository;
			_mapper = mapper;
			_unitOfWork = unitOfWork;
			_currentUserService = currentUserService;
			_userVoucherRepository = userVoucherRepository;
			_userRepository = userRepository;
		}

		public async Task<int> CreateVoucherCampaignAsync(RequestCreateVoucherCompaign voucherCompaign)
		{
			int userId = _currentUserService.GetUserId();
			var user = await _userRepository.GetUserById(userId);
			if (user.Role.RoleName.ToLower() != "admin")
			{
				throw new Exception("You do not have permission to create vouchers");
			}

			if (voucherCompaign == null)
			{
				throw new ArgumentNullException("Invalid voucher campaign");
			}
			if (voucherCompaign.StartDate < DateTime.Now || voucherCompaign.EndDate < DateTime.Now)
			{
				throw new ArgumentException("Start date and end date must be in the future");
			}
			if (voucherCompaign.StartDate >= voucherCompaign.EndDate)
			{
				throw new ArgumentException("End date must be later than start date.");
			}
			if (voucherCompaign.StartDate.AddDays(1) > voucherCompaign.EndDate)
			{
				throw new ArgumentException("End date must be at least 1 day after start date");
			}
			if (voucherCompaign.MaximumUserCanGet >= voucherCompaign.Quantity)
			{
				throw new ArgumentException("Maximum user can get must be smaller than quantity");
			}

			VoucherCampaign newVoucherCampaign = _mapper.Map<VoucherCampaign>(voucherCompaign);
			newVoucherCampaign.CreatedDate = DateTime.Now;
			newVoucherCampaign.IsDeleted = false;
			newVoucherCampaign.TakenQuantity = 0;
			newVoucherCampaign.ExpiredDate = newVoucherCampaign.EndDate.AddDays(newVoucherCampaign.SetNumberExpirationDate);

			if (newVoucherCampaign.StartDate > DateTime.Now)
			{
				newVoucherCampaign.Status = VoucherCampaignEnum.Pending.ToString();
			}
			else if (newVoucherCampaign.StartDate < DateTime.Now)
			{
				newVoucherCampaign.Status = VoucherCampaignEnum.Active.ToString();
			}
			else if (newVoucherCampaign.EndDate < DateTime.Now)
			{
				newVoucherCampaign.Status = VoucherCampaignEnum.Expired.ToString();
			}

			await _voucherCampaignRepository.InsertAsync(newVoucherCampaign);

			return await _unitOfWork.SaveChanges();
		}

		public async Task<int> DeleteVoucherCampaignAsync(int voucherCampaignId)
		{
			int userId = _currentUserService.GetUserId();
			var user = await _userRepository.GetUserById(userId);
			if (user.Role.RoleName.ToLower() != "admin")
			{
				throw new Exception("You do not have permission to delete vouchers");
			}

			await _voucherCampaignRepository.DeleteVoucherCampaignAsync(voucherCampaignId);
			return await _unitOfWork.SaveChanges();
		}

		public async Task<int> UpdateVoucherCampaignAsync(int voucherCampaignId, RequestUpdateVoucherCampaign updateVoucher)
		{
			int userId = _currentUserService.GetUserId();
			var user = await _userRepository.GetUserById(userId);
			if (user.Role.RoleName.ToLower() != "admin")
			{
				throw new Exception("You do not have permission to update vouchers");
			}

			if (updateVoucher == null)
			{
				throw new ArgumentNullException("Invalid update voucher campaign data");
			}
			VoucherCampaign voucherCampaign = await _voucherCampaignRepository.GetByIdAsync(voucherCampaignId);
			if (voucherCampaign == null)
			{
				throw new Exception("Invalid voucher campaign ID");
			}
			if (voucherCampaign.Status == VoucherCampaignEnum.Expired.ToString()
				|| voucherCampaign.Status == AdminVoucherCampaignEnum.Close.ToString())
			{
				throw new Exception("Cannot update voucher campaign with Expired or Close status");
			}

			// Khi mà voucher campaign đang ở trạng thái Active
			if (voucherCampaign.Status == VoucherCampaignEnum.Active.ToString())
			{
				if (updateVoucher.StartDate != voucherCampaign.StartDate)
				{
					throw new Exception("Active voucher campaign cannot update start date");
				}
				if (updateVoucher.PercentDiscount != voucherCampaign.PercentDiscount)
				{
					throw new Exception("Active voucher campaign cannot update percent discount");
				}
				if (updateVoucher.MaximumMoneyDiscount != voucherCampaign.MaximumMoneyDiscount)
				{
					throw new Exception("Active voucher campaign cannot update maximum money discount");
				}
				if (updateVoucher.Quantity != voucherCampaign.Quantity)
				{
					throw new Exception("Active voucher campaign cannot update quantity");
				}
				if (updateVoucher.SetNumberExpirationDate.HasValue)
				{
					throw new Exception("Avtice voucher campaign cannot update set number expiration date");
				}
			}

			// Khi mà voucher campaign đang ở trạng thái Pending
			if (voucherCampaign.Status == VoucherCampaignEnum.Pending.ToString())
			{
				if (updateVoucher.StartDate < voucherCampaign.StartDate || updateVoucher.EndDate < voucherCampaign.StartDate)
				{
					throw new ArgumentException("Start date and end date invalid");
				}
				if (updateVoucher.StartDate >= updateVoucher.EndDate)
				{
					throw new ArgumentException("End date must be later than start date.");
				}
				if (updateVoucher.StartDate.AddDays(1) > updateVoucher.EndDate)
				{
					throw new ArgumentException("End date must be at least 1 day after start date");
				}
			}

			voucherCampaign.StartDate = updateVoucher.StartDate;
			voucherCampaign.EndDate = updateVoucher.EndDate;
			voucherCampaign.Quantity = updateVoucher.Quantity;
			voucherCampaign.PercentDiscount = updateVoucher.PercentDiscount;
			voucherCampaign.MaximumMoneyDiscount = updateVoucher.MaximumMoneyDiscount;

			if (updateVoucher.Status.HasValue)
			{
				voucherCampaign.Status = updateVoucher.Status.Value.ToString();
			}
			if (updateVoucher.SetNumberExpirationDate.HasValue)
			{
				voucherCampaign.SetNumberExpirationDate = updateVoucher.SetNumberExpirationDate.Value;
				voucherCampaign.ExpiredDate = updateVoucher.EndDate.AddDays(updateVoucher.SetNumberExpirationDate.Value);
			}

			await _voucherCampaignRepository.UpdateAsync(voucherCampaign);

			return await _unitOfWork.SaveChanges();
		}

		public Task ViewVoucherCampaign()
		{
			throw new NotImplementedException();
		}

		public async Task BackGroundUpdateVoucherCampaign()
		{
			try
			{
				var listVoucherCampaign = await _voucherCampaignRepository.GetAllVoucherCampaign();
				if (listVoucherCampaign.Count != 0)
				{
					var updateListVoucherCampaign = new List<VoucherCampaign>();

					foreach (var voucherCampaign in listVoucherCampaign)
					{
						if (voucherCampaign.Status == AdminVoucherCampaignEnum.Close.ToString())
						{
							continue;
						}
						if (voucherCampaign.StartDate <= DateTime.Now && DateTime.Now <= voucherCampaign.EndDate)
						{
							voucherCampaign.Status = VoucherCampaignEnum.Active.ToString();
							updateListVoucherCampaign.Add(voucherCampaign);
						}
						else if (voucherCampaign.EndDate <= DateTime.Now)
						{
							voucherCampaign.Status = VoucherCampaignEnum.Expired.ToString();
							updateListVoucherCampaign.Add(voucherCampaign);
						}
					}

					if (updateListVoucherCampaign.Any())
					{
						_voucherCampaignRepository.UpdateRangeAsync(listVoucherCampaign);
						await _unitOfWork.SaveChanges();
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error updating voucher campaigns: {ex.Message}");
			}
		}

		public async Task<List<ResponseVoucherCampaign>> GetAllVoucherCampaign()
		{
			var listVoucherCampaign = await _voucherCampaignRepository.GetAllVoucherCampaign();
			return _mapper.Map<List<ResponseVoucherCampaign>>(listVoucherCampaign) ?? [];
		}

		public async Task<ResponseVoucherCampaign> GetVoucherCampaignById(int voucherCampaignId)
		{
			VoucherCampaign voucherCampaign = await _voucherCampaignRepository.GetByIdAsync(voucherCampaignId);
			if (voucherCampaign == null)
			{
				throw new KeyNotFoundException("Invalid voucher campaign id");
			}
			return _mapper.Map<ResponseVoucherCampaign>(voucherCampaign);
		}

		public async Task<List<ResponseVoucherCampaignBaseUser>> GetAllVoucherCampaignBaseCustomer()
		{
			int userId = _currentUserService.GetUserId();
			var listVoucherCampaign = await _voucherCampaignRepository.GetAllVoucherCampaign();
			var listUserVoucher = await _userVoucherRepository.GetAllCollectedVoucherCampaignIdByUserId(userId);

			var result = listVoucherCampaign.Select(x => new ResponseVoucherCampaignBaseUser
			{
				VoucherCampaignId = x.VoucherCampaignId,
				Name = x.Name,
				StartDate = x.StartDate,
				EndDate = x.EndDate,
				Quantity = x.Quantity,
				TakenQuantity = x.TakenQuantity,
				MaximumUserCanGet = x.MaximumUserCanGet,
				PercentDiscount = x.PercentDiscount,
				MaximumMoneyDiscount = x.MaximumMoneyDiscount,
				IsCollected = listUserVoucher.Contains(x.VoucherCampaignId),
			}).ToList();

			return result;
		}

		public async Task<VoucherCampaign> GetVoucherCampaignEntityById(int voucherCampaignId)
		{
			VoucherCampaign voucherCampaign = await _voucherCampaignRepository.GetByIdAsync(voucherCampaignId);
			if (voucherCampaign == null)
			{
				throw new KeyNotFoundException("Invalid voucher campaign id");
			}
			return voucherCampaign;
		}
	}
}
