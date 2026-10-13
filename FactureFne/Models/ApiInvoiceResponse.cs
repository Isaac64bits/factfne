using System;
using System.Collections.Generic;

namespace FactureFne.Models
{
    public class ApiInvoiceResponse
    {
        public string Ncc { get; set; }
        public string Reference { get; set; }
        public string Token { get; set; }
        public bool Warning { get; set; }
        public decimal BalanceFunds { get; set; }
        public InvoiceDetail Invoice { get; set; }
    }

    public class InvoiceDetail
    {
        public string Id { get; set; }
        public string Reference { get; set; }
        public string Type { get; set; }
        public string Subtype { get; set; }
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal FiscalStamp { get; set; }
        public decimal TotalBeforeTaxes { get; set; }
        public decimal TotalAfterTaxes { get; set; }
        public string ClientNcc { get; set; }
        public string ClientCompanyName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientEmail { get; set; }
        public string ClientSellerName { get; set; }
        public string ClientEstablishment { get; set; }
        public string ClientPointOfSale { get; set; }
        public string Status { get; set; }
        public string Template { get; set; }
        public string Footer { get; set; }
        public string CommercialMessage { get; set; }
        public List<ItemDetail> Items { get; set; }
    }

    public class ItemDetail
    {
        public string Id { get; set; }
        public decimal Quantity { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string MeasurementUnit { get; set; }
        public List<TaxDetail> Taxes { get; set; }
    }

    public class TaxDetail
    {
        public string InvoiceItemId { get; set; }
        public string VatRateId { get; set; }
        public decimal Amount { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
    }
}
