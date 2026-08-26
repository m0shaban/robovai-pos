using System;
using System.Collections.Generic;
using SmartPOS.Core.Entities;

namespace SmartPOS.Application.DTOs;

public class ParkedOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime ParkedAt { get; set; } = DateTime.Now;
    public List<CartItem> Items { get; set; } = new();
    public Customer? Customer { get; set; }
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string TimeFormatted => ParkedAt.ToString("hh:mm tt");
    public int ItemCount => Items.Count;
}
