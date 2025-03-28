﻿using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Mvc;
using PreOrderBlindBox.Data.Commons;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.Enum;
using PreOrderBlindBox.Data.IRepositories;
using PreOrderBlindBox.Data.UnitOfWork;
using PreOrderBlindBox.Services.DTO.RequestDTO.PreorderCampaignModel;
using PreOrderBlindBox.Services.DTO.RequestDTO.PreorderMilestoneModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.BlindBoxModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.ImageModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.PreorderCampaignModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.PreorderMilestoneModel;
using PreOrderBlindBox.Services.IServices;
using System.Linq.Expressions;
using static System.Net.Mime.MediaTypeNames;

namespace PreOrderBlindBox.Services.Services
{
    public class PreorderCampaignService : IPreorderCampaignService
    {
        private readonly IPreorderCampaignRepository _preorderCampaignRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPreorderMilestoneService _preorderMilestoneService;
        private readonly IBlindBoxRepository _blindBoxRepo;
        private readonly IMapper _mapper;
        private readonly IImageRepository _imageRepo;

        public PreorderCampaignService(IPreorderCampaignRepository preorderCampaignRepo
            , IUnitOfWork unitOfWork
            , IPreorderMilestoneService preorderMilestoneService
            , IBlindBoxRepository blindBoxRepo
            , IMapper mapper
            , IImageRepository imageRepo)
        {
            _preorderCampaignRepo = preorderCampaignRepo;
            _unitOfWork = unitOfWork;
            _preorderMilestoneService = preorderMilestoneService;
            _blindBoxRepo = blindBoxRepo;
            _mapper = mapper;
            _imageRepo = imageRepo;
        }

        private async Task<ResponseImageSplit> BuildResponseImageSplit(int blindBoxId)
        {
            var mainImage = await _imageRepo.GetMainImageByBlindBoxID(blindBoxId);
            var galleryImages = await _imageRepo.GetAllImageByBlindBoxID(blindBoxId);

            return new ResponseImageSplit
            {
                MainImage = mainImage != null
                    ? new ResponseImageModel
                    {
                        ImageId = mainImage.ImageId,
                        Url = mainImage.Url,
                        IsMainImage = mainImage.IsMainImage,
                        CreatedAt = mainImage.CreatedAt
                    }
                    : null,
                GalleryImages = galleryImages
                    .Where(img => !img.IsMainImage)
                    .Select(img => new ResponseImageModel
                    {
                        ImageId = img.ImageId,
                        Url = img.Url,
                        IsMainImage = img.IsMainImage,
                        CreatedAt = img.CreatedAt
                    }).ToList()
            };
        }


        public async Task<Pagination<ResponsePreorderCampaign>> GetAllValidPreorderCampaign(PaginationParameter page, PreorderCampaignGetRequest request)
        {
            string? type = null;
            if (request.Type.HasValue && Enum.IsDefined(typeof(PreorderCampaignType), request.Type.Value))
            {
                type = request.Type.Value.ToString();
            }

            var campaigns = await _preorderCampaignRepo.GetAllValidPreorderCampaign(page, type);
            var result = new List<ResponsePreorderCampaign>();

            foreach (var campaign in campaigns)
            {
                var responseCampaign = new ResponsePreorderCampaign
                {
                    PreorderCampaignId = campaign.PreorderCampaignId,
                    Slug = campaign.Slug,
                    StartDate = campaign.StartDate,
                    EndDate = campaign.EndDate,
                    Type = campaign.Type,
                    Status = campaign.Status,
                    PlacedOrderCount = campaign.PlacedOrderCount
                };

                // Lấy danh sách milestone và tính tổng số lượng
                var milestoneList = await _preorderMilestoneService.GetAllPreorderMilestoneByPreorderCampaignID(campaign.PreorderCampaignId);
                responseCampaign.TotalQuantity = milestoneList.Sum(m => m.Quantity);

                // Xử lý BlindBox nếu có
                if (campaign.BlindBox != null)
                {
                    // Mapping BlindBox (không chứa hình ảnh)
                    var blindBox = campaign.BlindBox;
                    var responseBlindBox = _mapper.Map<ResponseBlindBox>(blindBox);

                    // Lấy hình ảnh qua ImageRepository
                    responseBlindBox.Images = await BuildResponseImageSplit(blindBox.BlindBoxId);
                    responseCampaign.BlindBox = responseBlindBox;
                }

                result.Add(responseCampaign);
            }

            var countItem = _preorderCampaignRepo.Count(x => !x.IsDeleted);
            return new Pagination<ResponsePreorderCampaign>(result, countItem, page.PageIndex, page.PageSize);
        }

        // Helper method cho TimedPricing
        private decimal CalculateTimedPricingPrice(List<PreorderMilestone> orderedMilestones, int placedOrderCount)
        {
            decimal priceAtTime = 0m;
            int cumulativeQuantity = 0;
            // Chạy vòng lặp, cộng dồn quantity, nếu PlacedOrderCount nằm trong khoảng mốc, lấy price của mốc đó
            foreach (var milestone in orderedMilestones)
            {
                cumulativeQuantity += milestone.Quantity;
                if (placedOrderCount <= cumulativeQuantity)
                {
                    priceAtTime = milestone.Price;
                    break;
                }
            }
            // Nếu số lượng đặt vượt qua tổng cuối cùng của milestones, lấy giá của mốc cuối
            if (placedOrderCount > cumulativeQuantity && orderedMilestones.Any())
            {
                priceAtTime = orderedMilestones.Last().Price;
            }
            return priceAtTime;
        }

        // Helper method cho BulkOrder
        private decimal CalculateBulkOrderPrice(List<PreorderMilestone> orderedMilestones, int placedOrderCount)
        {
            // Tạo danh sách cumulativeQuantities
            // cumulativeQuantities[i] = tổng quantity của milestone từ 0 đến i
            decimal priceAtTime = 0m;
            var cumulativeQuantities = new List<int>();
            int runningSum = 0;
            foreach (var milestone in orderedMilestones)
            {
                runningSum += milestone.Quantity;
                cumulativeQuantities.Add(runningSum);
            }
            // Vòng lặp xác định priceAtTime dựa trên PlacedOrderCount
            // Ta sẽ duyệt đến mốc kế tiếp để so sánh
            for (int i = 0; i < orderedMilestones.Count; i++)
            {
                // Nếu chưa phải mốc cuối, so sánh với cumulativeQuantities[i+1]
                if (i < orderedMilestones.Count - 1)
                {
                    // Nếu PlacedOrderCount <= c[i+1], lấy giá milestone[i] rồi dừng
                    if (placedOrderCount <= cumulativeQuantities[i + 1])
                    {
                        priceAtTime = orderedMilestones[i].Price;
                        break;
                    }
                }
                else
                {
                    // Nếu chưa break, nghĩa là đặt vượt quá tất cả các mốc, dùng giá mốc cuối
                    priceAtTime = orderedMilestones[i].Price;
                }
            }
            return priceAtTime;
        }

        public async Task<Pagination<ResponsePreorderCampaign>> GetAllActivePreorderCampaign(PaginationParameter page, PreorderCampaignGetRequest request)
        {
            string? type = null;
            if (request.Type.HasValue && Enum.IsDefined(typeof(PreorderCampaignType), request.Type.Value))
            {
                type = request.Type.Value.ToString();
            }

            var campaigns = await _preorderCampaignRepo.FilterPreorderCampaignsAsync(type, request.isEndingSoon, request.isNewlyLaunched, request.isTrending, page);
            var result = new List<ResponsePreorderCampaign>();

            foreach (var campaign in campaigns)
            {
                var responseCampaign = new ResponsePreorderCampaign
                {
                    PreorderCampaignId = campaign.PreorderCampaignId,
                    Slug = campaign.Slug,
                    StartDate = campaign.StartDate,
                    EndDate = campaign.EndDate,
                    Type = campaign.Type,
                    Status = campaign.Status,
                    PlacedOrderCount = campaign.PlacedOrderCount
                };

                // Lấy danh sách milestone và tính tổng số lượng
                var milestoneList = await _preorderMilestoneService.GetAllPreorderMilestoneByPreorderCampaignID(campaign.PreorderCampaignId);
                responseCampaign.TotalQuantity = milestoneList.Sum(m => m.Quantity);

                var priceAtTime = 0m;
                // Sắp xếp milestones theo MilestoneNumber (hoặc tiêu chí bạn muốn)
                var orderedMilestones = milestoneList.OrderBy(m => m.MilestoneNumber).ToList();
                int placedOrderCount = campaign.PlacedOrderCount ?? 0; // Chỉ đề phòng Null
                if (campaign.Type == PreorderCampaignType.TimedPricing.ToString())
                {
                    priceAtTime = CalculateTimedPricingPrice(orderedMilestones, placedOrderCount);
                }

                if (campaign.Type == PreorderCampaignType.BulkOrder.ToString())
                {
                    priceAtTime = CalculateBulkOrderPrice(orderedMilestones, placedOrderCount);
                }
                responseCampaign.PriceAtTime = priceAtTime;

                var discountPercent = 0m;
                var discount = campaign.BlindBox.ListedPrice - priceAtTime;
                discountPercent = (discount / campaign.BlindBox.ListedPrice) * 100;

                responseCampaign.DiscountPercent = discountPercent;

                // Xử lý BlindBox nếu có
                if (campaign.BlindBox != null)
                {
                    // Mapping BlindBox (không chứa hình ảnh)
                    var blindBox = campaign.BlindBox;
                    var responseBlindBox = _mapper.Map<ResponseBlindBox>(blindBox);

                    // Lấy hình ảnh qua ImageRepository
                    responseBlindBox.Images = await BuildResponseImageSplit(blindBox.BlindBoxId);
                    responseCampaign.BlindBox = responseBlindBox;
                }

                result.Add(responseCampaign);
            }

            var countItem = _preorderCampaignRepo.Count(x => !x.IsDeleted);
            return new Pagination<ResponsePreorderCampaign>(result, countItem, page.PageIndex, page.PageSize);
        }


        public static string GenerateShortUniqueString()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                          .Replace("=", "")
                          .Replace("+", "")
                          .Replace("/", "");
        }

        public async Task<ResponsePreorderCampaignDetail?> GetPreorderCampaignAsyncById(int id)
        {
            // Lấy thông tin PreorderCampaign từ DB
            var preorderCampaign = await _preorderCampaignRepo.GetDetailPreorderCampaignById(id);

            if (preorderCampaign == null)
            {
                return null;
            }

            // Mapping BlindBox (không chứa hình ảnh)
            var blindBox = preorderCampaign.BlindBox;
            var responseBlindBox = _mapper.Map<ResponseBlindBox>(blindBox);

            if (preorderCampaign.BlindBox != null)
            {
                // Lấy hình ảnh qua ImageRepository
                responseBlindBox.Images = await BuildResponseImageSplit(blindBox.BlindBoxId);
            }

            // Ánh xạ sang ResponsePreorderCampaignDetail
            var response = new ResponsePreorderCampaignDetail
            {
                PreorderCampaignId = preorderCampaign.PreorderCampaignId,
                BlindBoxId = preorderCampaign.BlindBoxId,
                Slug = preorderCampaign.Slug,
                StartDate = preorderCampaign.StartDate,
                EndDate = preorderCampaign.EndDate,
                Status = preorderCampaign.Status,
                Type = preorderCampaign.Type,
                IsDeleted = preorderCampaign.IsDeleted,
                BlindBox = responseBlindBox
            };

            return response;
        }

        public async Task<ResponsePreorderCampaignDetail?> GetPreorderCampaignBySlugAsync(string slug)
        {
            var preorderCampaign = await _preorderCampaignRepo.GetPreorderCampaignBySlugAsync(slug);

            if (preorderCampaign == null)
            {
                return null;
            }

            // Mapping BlindBox (không chứa hình ảnh)
            var blindBox = preorderCampaign.BlindBox;
            var responseBlindBox = _mapper.Map<ResponseBlindBox>(blindBox);

            // Nếu có BlindBox, lấy danh sách hình ảnh của nó
            if (preorderCampaign.BlindBox != null)
            {
                responseBlindBox.Images = await BuildResponseImageSplit(blindBox.BlindBoxId);
            }

            var milestoneList = await _preorderMilestoneService.GetAllPreorderMilestoneByPreorderCampaignID(preorderCampaign.PreorderCampaignId);
            var quantityCount = milestoneList.Sum(m => m.Quantity);

              var priceAtTime = 0m;
            // Sắp xếp milestones theo MilestoneNumber (hoặc tiêu chí bạn muốn)
            var orderedMilestones = milestoneList.OrderBy(m => m.MilestoneNumber).ToList();
            int placedOrderCount = preorderCampaign.PlacedOrderCount ?? 0; // Chỉ đề phòng Null
            if (preorderCampaign.Type == PreorderCampaignType.TimedPricing.ToString())
            {
                priceAtTime = CalculateTimedPricingPrice(orderedMilestones, placedOrderCount);
            }

            if (preorderCampaign.Type == PreorderCampaignType.BulkOrder.ToString())
            {
                priceAtTime = CalculateBulkOrderPrice(orderedMilestones, placedOrderCount);
            }

            // Ánh xạ sang ResponsePreorderCampaignDetail
            var response = new ResponsePreorderCampaignDetail
            {
                PreorderCampaignId = preorderCampaign.PreorderCampaignId,
                BlindBoxId = preorderCampaign.BlindBoxId,
                Slug = preorderCampaign.Slug,
                StartDate = preorderCampaign.StartDate,
                EndDate = preorderCampaign.EndDate,
                Status = preorderCampaign.Status,
                Type = preorderCampaign.Type,
                IsDeleted = preorderCampaign.IsDeleted,
                PriceAtTime = priceAtTime,
                TotalQuantity = quantityCount,
                PlacedOrderCount = preorderCampaign.PlacedOrderCount,
                BlindBox = responseBlindBox,
                PreorderMilestones = milestoneList.Select(m => new ResponsePreorderMilestone
                {
                    PreorderMilestoneId = m.PreorderMilestoneId,
                    MilestoneNumber = m.MilestoneNumber,
                    Quantity = m.Quantity,
                    Price = m.Price,
                    PreorderCampaignId = m.PreorderCampaignId,
                }).OrderBy(m => m.MilestoneNumber).ToList()
            };

            return response;
        }

        public async Task<bool> DeletePreorderCampaign(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var preorderCampaign = await _preorderCampaignRepo.GetByIdAsync(id);

                if (preorderCampaign == null)
                {
                    return false;
                }

                if (!preorderCampaign.IsDeleted)
                {
                    // Đánh dấu PreorderCampaign là đã xóa
                    preorderCampaign.IsDeleted = true;
                    await _preorderCampaignRepo.UpdateAsync(preorderCampaign);

                    // Lấy danh sách tất cả PreorderMilestone liên quan
                    var milestones = await _preorderMilestoneService.GetAllPreorderMilestoneByPreorderCampaignID(id);
                    // Đánh dấu tất cả milestones là đã xóa
                    foreach (var milestone in milestones)
                    {
                        //milestone.IsDeleted = true;
                        await _preorderMilestoneService.DeletePreorderMilestone(milestone.PreorderMilestoneId);
                    }

                    // Lưu thay đổi vào database
                    await _unitOfWork.SaveChanges();
                    await _unitOfWork.CommitTransactionAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return false;
        }

        public async Task BackGroundUpdatePreorderCampaign()
        {
            try
            {
                var listPreorderCampaign = await _preorderCampaignRepo.GetAllPreorderCampaign();
                if (listPreorderCampaign.Count != 0)
                {
                    var updateListPreorderCampaign = new List<PreorderCampaign>();

                    foreach (var campaign in listPreorderCampaign)
                    {
                        if (campaign.Status == PreorderCampaignStatus.Canceled.ToString() ||
                            campaign.Status == PreorderCampaignStatus.Completed.ToString())
                        {
                            continue;
                        }

                        if (campaign.StartDate <= DateTime.Now && DateTime.Now <= campaign.EndDate)
                        {
                            if (campaign.Status != PreorderCampaignStatus.Active.ToString())
                            {
                                campaign.Status = PreorderCampaignStatus.Active.ToString();
                                campaign.UpdatedDate = DateTime.Now;
                                updateListPreorderCampaign.Add(campaign);
                            }
                        }
                        else if (campaign.EndDate < DateTime.Now)
                        {
                            if (campaign.Status != PreorderCampaignStatus.Completed.ToString())
                            {
                                campaign.Status = PreorderCampaignStatus.Completed.ToString();
                                campaign.UpdatedDate = DateTime.Now;
                                updateListPreorderCampaign.Add(campaign);
                            }
                        }
                    }

                    if (updateListPreorderCampaign.Any())
                    {
                        _preorderCampaignRepo.UpdateRangeAsync(updateListPreorderCampaign);
                        await _unitOfWork.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating preorder campaigns: {ex.Message}");
            }
        }

        public async Task<int> CancelPreorderCampaign(int id)
        {
            var preorderCampaign = await _preorderCampaignRepo.GetByIdAsync(id);

            if (preorderCampaign == null)
            {
                throw new ArgumentException("Pre-Order Campaign not found");
            }

            if (preorderCampaign.IsDeleted || preorderCampaign.Status == PreorderCampaignStatus.Completed.ToString())
            {
                throw new ArgumentException("Cannot update Pre-Order Campaign had deleted or completed");
            }

            if (preorderCampaign.Type == PreorderCampaignType.TimedPricing.ToString())
            {
                throw new ArgumentException("Cannot cancel TimedPricing campaign");
            }

            preorderCampaign.Status = PreorderCampaignStatus.Canceled.ToString();

            await _preorderCampaignRepo.UpdateAsync(preorderCampaign);

            // Lưu thay đổi vào database
            return await _unitOfWork.SaveChanges();
        }

        public async Task<Pagination<ResponseSearchPreorderCampaign>> SearchPreorderCampaignAsync(PreorderCampaignSearchRequest searchRequest, PaginationParameter pagination)
        {
            // Gọi repository để lấy danh sách PreorderCampaign theo yêu cầu
            var campaigns = await _preorderCampaignRepo.SearchPreorderCampaign(searchRequest.BlindBoxName, searchRequest.SortOrder.ToString(), pagination);

            var result = new List<ResponseSearchPreorderCampaign>();

            // Với mỗi campaign, thực hiện mapping và gọi riêng ImageRepository để lấy hình ảnh của BlindBox
            foreach (var campaign in campaigns)
            {
                // Tính khoảng giá dựa trên milestone của campaign
                var priceFrom = campaign.PreorderMilestones.Any()
                                    ? campaign.PreorderMilestones.Min(m => m.Price)
                                    : 0;
                var priceTo = campaign.PreorderMilestones.Any()
                                    ? campaign.PreorderMilestones.Max(m => m.Price)
                                    : 0;

                // Mapping BlindBox (không chứa hình ảnh)
                var blindBox = campaign.BlindBox;
                var responseBlindBox = _mapper.Map<ResponseBlindBox>(blindBox);

                // Lấy hình ảnh qua ImageRepository
                responseBlindBox.Images = await BuildResponseImageSplit(blindBox.BlindBoxId);

                result.Add(new ResponseSearchPreorderCampaign
                {
                    PreorderCampaignId = campaign.PreorderCampaignId,
                    BlindBoxId = campaign.BlindBoxId,
                    Slug = campaign.Slug,
                    StartDate = campaign.StartDate,
                    EndDate = campaign.EndDate,
                    Status = campaign.Status,
                    Type = campaign.Type,
                    IsDeleted = campaign.IsDeleted,
                    BlindBox = responseBlindBox,
                    PriceFrom = priceFrom,
                    PriceTo = priceTo,
                });
            }
            var countItem = _preorderCampaignRepo.Count(x => x.Status == "Active");
            return new Pagination<ResponseSearchPreorderCampaign>(result, countItem, pagination.PageIndex, pagination.PageSize);
        }

        public async Task<bool> AddCampaignWithMilestonesAsync(CreatePreorderCampaignRequest campaignRequest)
        {
            // Kiểm tra dữ liệu đầu vào
            if (campaignRequest == null)
                throw new ArgumentNullException("Campaign request cannot be null.");

            // Các kiểm tra tương tự như trong AddPreorderCampaignAsync
            if (campaignRequest.EndDate < campaignRequest.StartDate)
            {
                throw new ArgumentException("End date cannot be earlier than start date.");
            }
            if (campaignRequest.EndDate <= DateTime.Now || campaignRequest.StartDate < DateTime.Now)
            {
                throw new ArgumentException("Start date and end date must be in future.");
            }
            if (campaignRequest.StartDate.AddDays(5) > campaignRequest.EndDate)
            {
                throw new ArgumentException("End date must be at least 5 days after start date.");
            }
            if (!Enum.IsDefined(typeof(PreorderCampaignType), campaignRequest.Type))
            {
                throw new ArgumentException("Invalid campaign type. Must be TimedPricing (0) or BulkOrder (1).");
            }

            // Kiểm tra BlindBox
            var blindBox = await _blindBoxRepo.GetDetailBlindBoxById(campaignRequest.BlindBoxId.Value);
            if (blindBox == null || blindBox.IsDeleted)
            {
                throw new ArgumentException("Blind box does not exist or has been deleted.");
            }

            // Bắt đầu giao dịch
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Tạo đối tượng campaign
                var campaign = new PreorderCampaign
                {
                    BlindBoxId = campaignRequest.BlindBoxId,
                    Slug = GenerateShortUniqueString(),
                    StartDate = campaignRequest.StartDate,
                    EndDate = campaignRequest.EndDate,
                    Status = PreorderCampaignStatus.Pending.ToString(),
                    Type = campaignRequest.Type.ToString(),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    IsDeleted = false
                };

                await _preorderCampaignRepo.InsertAsync(campaign);
                await _unitOfWork.SaveChanges(); // Giả sử sau SaveChanges, campaign.Id được gán

                var milestoneList = new List<CreatePreorderMilestoneRequest>();
                // Lặp qua từng milestone, gán PreorderCampaignId và tạo milestone
                for (int i = 0; i < campaignRequest.MilestoneRequests.Count; i++)
                {
                    var listedPrice = blindBox.ListedPrice;

                    if (campaignRequest.MilestoneRequests[i].Price >= listedPrice)
                    {
                        throw new ArgumentException("Price in campaign cannot greater than or equal with listed price");
                    }
                    var milestone = new CreatePreorderMilestoneRequest
                    {
                        PreorderCampaignId = campaign.PreorderCampaignId,
                        MilestoneNumber = i + 1,
                        Quantity = campaignRequest.MilestoneRequests[i].Quantity,
                        Price = campaignRequest.MilestoneRequests[i].Price
                    };
                    milestoneList.Add(milestone);
                    //await _preorderMilestoneService.AddPreorderMilestoneAsync(milestone);
                }
                await _preorderMilestoneService.AddPreorderMilestonesAsync(milestoneList);

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> UpdatePreorderCampaignWithMilestone(int id, UpdatePreorderCampaignRequest request)
        {
            var preorderCampaign = await _preorderCampaignRepo.GetByIdAsync(id);

            if (preorderCampaign == null)
            {
                throw new ArgumentException("Pre-Order Campaign not found");
            }

            if (request == null)
            {
                throw new ArgumentNullException("Invalid update Pre-Order Campaign data");
            }

            if (preorderCampaign.IsDeleted || preorderCampaign.Status == PreorderCampaignStatus.Active.ToString()
                || preorderCampaign.Status == PreorderCampaignStatus.Completed.ToString())
            {
                throw new ArgumentException("Cannot update Pre-Order Campaign had deleted or active or completed");
            }

            if (request.EndDate < request.StartDate)
            {
                throw new ArgumentException("End date cannot be earlier than start date.");
            }

            if (request.EndDate <= DateTime.Now || request.StartDate < DateTime.Now)
            {
                throw new ArgumentException("Start date and end date must be in future");
            }

            if (request.StartDate.AddDays(5) > request.EndDate)
            {
                throw new ArgumentException("End date must be at least 5 day after start date");
            }

            // Kiểm tra giá trị enum
            if (!Enum.IsDefined(typeof(PreorderCampaignType), request.Type))
            {
                throw new ArgumentException("Invalid campaign type. Must be TimedPricing (0) or BulkOrder (1).");
            }

            var blindBox = await _blindBoxRepo.GetDetailBlindBoxById(preorderCampaign.BlindBoxId.Value);
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _mapper.Map(request, preorderCampaign);
                await _preorderCampaignRepo.UpdateAsync(preorderCampaign);
                await _unitOfWork.SaveChanges();

                if (request.PreorderMilestoneRequests.Count > 0)
                {
                    // Lấy danh sách tất cả PreorderMilestone liên quan
                    var milestones = await _preorderMilestoneService.GetAllPreorderMilestoneByPreorderCampaignID(id);
                    // Đánh dấu tất cả milestones là đã xóa
                    foreach (var milestone in milestones)
                    {
                        //milestone.IsDeleted = true;
                        await _preorderMilestoneService.DeletePreorderMilestone(milestone.PreorderMilestoneId);
                    }

                    // Lặp qua từng milestone, gán PreorderCampaignId và tạo milestone
                    var milestoneAddList = new List<CreatePreorderMilestoneRequest>();
                    for (int i = 0; i < request.PreorderMilestoneRequests.Count; i++)
                    {
                        var listedPrice = blindBox.ListedPrice;
                        if (request.PreorderMilestoneRequests[i].Price >= listedPrice)
                        {
                            throw new ArgumentException("Price in campaign cannot greater than or equal with listed price");
                        }
                        var milestone = new CreatePreorderMilestoneRequest
                        {
                            PreorderCampaignId = preorderCampaign.PreorderCampaignId,
                            MilestoneNumber = i + 1,
                            Quantity = (int)request.PreorderMilestoneRequests[i].Quantity,
                            Price = (decimal)request.PreorderMilestoneRequests[i].Price
                        };
                        milestoneAddList.Add(milestone);
                        //await _preorderMilestoneService.AddPreorderMilestoneAsync(milestone);
                    }
                    await _preorderMilestoneService.AddPreorderMilestonesAsync(milestoneAddList);
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

        public async Task<Pagination<ResponsePreorderCampaignDetail>> GetAllCompleteBulkCampaign (PaginationParameter pagination)
        {
            // Lấy danh sách campaign bulk đã hoàn thành theo phân trang
            var campaigns = await _preorderCampaignRepo.GetAllCompleteBulkPreorderCampaign(pagination);
            var responseList = new List<ResponsePreorderCampaignDetail?>();

            foreach (var campaign in campaigns)
            {
                if (campaign == null)
                    continue;
                var blindBox = campaign.BlindBox;
                var responseBlindBox = _mapper.Map<ResponseBlindBox>(blindBox);

                // Nếu có BlindBox, lấy thông tin hình ảnh
                if (campaign.BlindBox != null)
                {
                    responseBlindBox.Images = await BuildResponseImageSplit(campaign.BlindBox.BlindBoxId);
                }

                // Lấy danh sách PreorderMilestones (đã include trong repository)
                var milestoneList = campaign.PreorderMilestones;
                var totalQuantity = milestoneList.Sum(m => m.Quantity);
                decimal priceAtTime = 0m;
                // Sắp xếp milestones theo MilestoneNumber
                var orderedMilestones = milestoneList.OrderBy(m => m.MilestoneNumber).ToList();
                int placedOrderCount = campaign.PlacedOrderCount ?? 0;

                // Vì ở repository đã lọc theo BulkOrder nên chúng ta chỉ cần xử lý nhánh BulkOrder
                if (campaign.Type == PreorderCampaignType.BulkOrder.ToString())
                {
                    priceAtTime = CalculateBulkOrderPrice(orderedMilestones, placedOrderCount);
                }

                // Ánh xạ sang ResponsePreorderCampaignDetail
                var responseDetail = new ResponsePreorderCampaignDetail
                {
                    PreorderCampaignId = campaign.PreorderCampaignId,
                    BlindBoxId = campaign.BlindBoxId,
                    Slug = campaign.Slug,
                    StartDate = campaign.StartDate,
                    EndDate = campaign.EndDate,
                    Status = campaign.Status,
                    Type = campaign.Type,
                    IsDeleted = campaign.IsDeleted,
                    PriceAtTime = priceAtTime,
                    TotalQuantity = totalQuantity,
                    PlacedOrderCount = campaign.PlacedOrderCount,
                    BlindBox = responseBlindBox,
                    PreorderMilestones = milestoneList
                        .Select(m => new ResponsePreorderMilestone
                        {
                            PreorderMilestoneId = m.PreorderMilestoneId,
                            MilestoneNumber = m.MilestoneNumber,
                            Quantity = m.Quantity,
                            Price = m.Price,
                            PreorderCampaignId = m.PreorderCampaignId
                        }) 
                        .OrderBy(m => m.MilestoneNumber)
                        .ToList()
                };

                responseList.Add(responseDetail);
            }

            var countItem = _preorderCampaignRepo.Count(x => x.Status == PreorderCampaignStatus.Completed.ToString());
            return new Pagination<ResponsePreorderCampaignDetail>(responseList, countItem, pagination.PageIndex, pagination.PageSize);
        }

        public async Task<List<ResponseSearchPreorderCampaign>> GetSimilarPreorderCampaign(int id)
        {
            // Gọi repository để lấy danh sách PreorderCampaign theo yêu cầu
            var campaigns = await _preorderCampaignRepo.GetSimilarPreorderCampaign(id);

            var result = new List<ResponseSearchPreorderCampaign>();

            // Với mỗi campaign, thực hiện mapping và gọi riêng ImageRepository để lấy hình ảnh của BlindBox
            foreach (var campaign in campaigns)
            {
                // Tính khoảng giá dựa trên milestone của campaign
                var priceFrom = campaign.PreorderMilestones.Any()
                                    ? campaign.PreorderMilestones.Min(m => m.Price)
                                    : 0;
                var priceTo = campaign.PreorderMilestones.Any()
                                    ? campaign.PreorderMilestones.Max(m => m.Price)
                                    : 0;

                // Mapping BlindBox (không chứa hình ảnh)
                var blindBox = campaign.BlindBox;
                var responseBlindBox = _mapper.Map<ResponseBlindBox>(blindBox);

                responseBlindBox.Images = await BuildResponseImageSplit(blindBox.BlindBoxId);

                result.Add(new ResponseSearchPreorderCampaign
                {
                    PreorderCampaignId = campaign.PreorderCampaignId,
                    BlindBoxId = campaign.BlindBoxId,
                    Slug = campaign.Slug,
                    StartDate = campaign.StartDate,
                    EndDate = campaign.EndDate,
                    Status = campaign.Status,
                    Type = campaign.Type,
                    IsDeleted = campaign.IsDeleted,
                    BlindBox = responseBlindBox,
                    PriceFrom = priceFrom,
                    PriceTo = priceTo,
                });
            }
            return result;
        }

    }
}
