﻿using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Services.DTO.RequestDTO.PreorderMilestoneModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOrderBlindBox.Services.IServices
{
    public interface IPreorderMilestoneService
    {
        Task<PreorderMilestone?> GetPreorderMilestoneById(int id);
        Task<PreorderMilestone> AddPreorderMilestoneAsync(CreatePreorderMilestoneRequest createPreorderMilestoneRequest);
        Task<bool> DeletePreorderMilestone(int id);
        Task<PreorderMilestone?> UpdatePreorderMilestone(int id, UpdatePreorderMilestoneRequest request);
        Task<List<PreorderMilestone>> GetAllPreorderMilestoneByCampaignID(int campaignID);
        Task<int> CalculateRemainingQuantity(int milestoneID, int quantityOrderDetails);
    }
}
