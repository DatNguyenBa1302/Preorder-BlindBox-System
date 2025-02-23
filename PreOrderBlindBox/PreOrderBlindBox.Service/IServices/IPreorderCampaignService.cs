﻿using PreOrderBlindBox.Data.Commons;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.Enum;
using PreOrderBlindBox.Services.DTO.RequestDTO.PreorderCampaignModel;
using PreOrderBlindBox.Services.DTO.ResponeDTO.PreorderCampaignModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOrderBlindBox.Services.IServices
{
    public interface IPreorderCampaignService
    {
        Task<Pagination</*PreorderCampaign*/ResponsePreorderCampaign>> GetAllActivePreorderCampaign(PaginationParameter page);
        Task<int> AddPreorderCampaignAsync(CreatePreorderCampaignRequest createPreorderCampaignRequest);
        Task<ResponsePreorderCampaignDetail?> GetPreorderCampaignAsyncById(int id);
        Task<ResponsePreorderCampaignDetail?> GetPreorderCampaignBySlugAsync(string slug);
        Task<bool> DeletePreorderCampaign(int id);
        Task<int> UpdatePreorderCampaign(int id, UpdatePreorderCampaignRequest request);
        Task BackGroundUpdatePreorderCampaign();
        Task<int> CancelPreorderCampaign(int id, CancelPreorderCampaignRequest request);
        //Task<List<ResponseSearchPreorderCampaign>> SearchPreorderCampaignAsync(PreorderCampaignSearchRequest searchRequest);
        Task<Pagination<ResponseSearchPreorderCampaign>> SearchPreorderCampaignAsync(PreorderCampaignSearchRequest searchRequest, PaginationParameter pagination);
    }
}
