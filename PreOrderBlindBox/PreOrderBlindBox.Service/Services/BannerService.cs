﻿
using CurcusProject.CM.Helpers;
using Microsoft.AspNetCore.Http;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.IRepositories;
using PreOrderBlindBox.Data.Repositories;
using PreOrderBlindBox.Data.UnitOfWork;
using PreOrderBlindBox.Services.DTO.RequestDTO.BannerModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.ImageModel;
using PreOrderBlindBox.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace PreOrderBlindBox.Services.Services
{
    public class BannerService : IBannerService
    {
        private readonly IBannerRepository _bannerRepo;
        private readonly IBlobService _blobService;
        private readonly IUnitOfWork _unitOfWork;

        public BannerService(IBannerRepository bannerRepo, IBlobService blobService, IUnitOfWork unitOfWork)
        {
            _bannerRepo = bannerRepo;
            _blobService = blobService;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateBanner(CreateBannerRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (request.File == null)
                    return 0;

                var file = request.File;
                var imageUrl = await _blobService.UploadFile(file);
                if (string.IsNullOrEmpty(imageUrl)) return 0;

                var banner = new Banner
                {
                    ImageUrl = imageUrl,
                    Title = request.Title,
                    CallToActionUrl = request.CallToActionUrl,
                    Priority = request.Priority,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                await _bannerRepo.InsertAsync(banner);
                return await _unitOfWork.SaveChanges();
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return 0;
            }
        }

        public async Task<int> DeleteBanner(int id)
        {
            var banner = await _bannerRepo.GetByIdAsync(id);
            if (banner == null) return 0;

            await _bannerRepo.Delete(banner);

            // Xóa file trên Blob Storage
            var fileName = Path.GetFileName(banner.ImageUrl);
            await _blobService.DeleteFile(fileName);

            return await _unitOfWork.SaveChanges();
        }

        public async Task<Banner?> GetBannerById(int id)
        {
            return await _bannerRepo.GetByIdAsync(id);
        }

        public async Task<int> UpdateBanner(int id, UpdateBannerRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var banner = await _bannerRepo.GetByIdAsync(id);
                if (banner == null) return 0;

                var imageUrl = "";
                if (request.File != null)
                {
                    //xoa anh cu
                    var fileName = Path.GetFileName(banner.ImageUrl);
                    await _blobService.DeleteFile(fileName);

                    //them anh moi
                    var file = request.File;
                    imageUrl = await _blobService.UploadFile(file);
                    if (string.IsNullOrEmpty(imageUrl)) return 0;
                }

                var newBanner = new Banner
                {
                    ImageUrl = imageUrl,
                    Title = request.Title,
                    CallToActionUrl = request.CallToActionUrl,
                    Priority = request.Priority,
                    UpdatedDate = DateTime.Now
                };

                await _bannerRepo.UpdateAsync(banner);
                return await _unitOfWork.SaveChanges();
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return 0;
            }
        }
    }
}
