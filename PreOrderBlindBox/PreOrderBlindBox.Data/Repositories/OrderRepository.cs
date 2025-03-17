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
	public class OrderRepository : GenericRepository<Order>, IOrderRepository
	{
		public OrderRepository(Preorder_BlindBoxContext context) : base(context)
		{
		}

		public async Task<List<RevenueDto>> GetListRevenueByTime(DateTime fromDate, DateTime toDate)
		{
			return await _context.Orders.Where(x => x.CreatedDate.Date >= fromDate && x.CreatedDate.Date <= toDate)
				.GroupBy(x => x.CreatedDate.Date)
				.Select(group => new RevenueDto
				{
					Date = group.Key,
					TotalRevenue = group.Sum(x => x.Amount)
				})
				.ToListAsync();
		}
	}

	public class RevenueDto
	{
		public DateTime Date { get; set; }
		public decimal TotalRevenue { get; set; }
	}
}
