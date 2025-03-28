﻿using Microsoft.EntityFrameworkCore;
using PreOrderBlindBox.Data.DBContext;
using PreOrderBlindBox.Data.Entities;
using PreOrderBlindBox.Data.GenericRepository;
using PreOrderBlindBox.Data.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOrderBlindBox.Data.Repositories
{
    public class ImageRepository : GenericRepository<Image>, IImageRepository
    {
        public ImageRepository(Preorder_BlindBoxContext context) : base(context)
        {
        }

        public async Task<Image> GetMainImageByBlindBoxID(int blindBoxID)
        {
            return await _context.Images.FirstOrDefaultAsync(x => x.BlindBoxId == blindBoxID && x.IsMainImage);
        }

        public async Task<List<Image>> GetAllImageByBlindBoxID(int blindBoxId)
        {
            var result = await GetAll(filter: x => x.BlindBoxId == blindBoxId);
            return result;
        }
    }
}
