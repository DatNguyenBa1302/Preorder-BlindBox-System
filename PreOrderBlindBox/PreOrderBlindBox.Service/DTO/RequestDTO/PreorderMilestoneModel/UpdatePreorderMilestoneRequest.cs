﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOrderBlindBox.Services.DTO.RequestDTO.PreorderMilestoneModel
{
    public class UpdatePreorderMilestoneRequest
    {
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }
}
