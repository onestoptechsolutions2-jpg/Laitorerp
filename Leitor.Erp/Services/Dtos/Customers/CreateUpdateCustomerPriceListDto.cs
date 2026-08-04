using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateUpdateCustomerPriceListDto
{
    [Required]
    public Guid PriceListId { get; set; }

    public bool IsPrimary { get; set; }
}
