using System;

namespace APIServer.Model;

public class Order
{
    public Guid OrderID { get; set; } = Guid.NewGuid();
    public string? ItemName { get; set; }
    public int Quantity { get; set; }
    public string? Status { get; set; }
    public double Price { get; set; }
}