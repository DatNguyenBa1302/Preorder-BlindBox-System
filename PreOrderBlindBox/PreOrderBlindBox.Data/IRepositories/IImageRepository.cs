﻿using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.GenericRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOrderBlindBox.Data.IRepositories
{
    public interface IImageRepository : IGenericRepository<Image>
    {
        public Task<Image> GetMainImageByBlindBoxID(int blindBoxID);
        Task<List<Image>> GetAllImageByBlindBoxID(int blindBoxId);
    }
}
