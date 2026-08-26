namespace SmartPOS.Core.Entities;

/// <summary>
/// Sale transaction header
/// </summary>
public class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public string? Notes { get; set; }
    public int? LoyaltyPointsEarned { get; set; }
    public int? LoyaltyPointsRedeemed { get; set; }
    public string? QRCode { get; set; }
    public bool IsPrinted { get; set; }

    // Foreign Keys
    public int UserId { get; set; }
    public int? ConsumedByUserId { get; set; } // الموظف الذي استهلك وجبة الضيافة
    public int? CustomerId { get; set; }
    public int? ShiftId { get; set; }
    public int? TableId { get; set; }
    public OrderType OrderType { get; set; } = OrderType.DineIn;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual User? ConsumedByUser { get; set; } // رابط بجدول المستخدمين
    public virtual Customer? Customer { get; set; }
    public virtual Shift? Shift { get; set; }
    public virtual Table? Table { get; set; }
    public virtual ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();
    public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

    // Computed Properties
    public int TotalItems => SaleDetails.Sum(d => d.Quantity);
    public decimal NetProfit => SaleDetails.Sum(d => d.LineProfit);
    public decimal Total => TotalAmount;
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    MobileMoney = 4,
    Split = 5,
    VodafoneCash = 6,
    InstaPay = 7,
    Deferred = 8,
    StaffMeal = 9,           // وجبة ضيافة للموظفين
    Mada = 10,               // بطاقة مدى (السعودية)
    StcPay = 11,             // STC Pay (السعودية)
    ApplePay = 12,           // Apple Pay (عام / الخليج)
    Tamara = 13,             // تمارا (تقسيط السعودية والإمارات)
    Tabby = 14,              // تابي (تقسيط السعودية والإمارات)
    Urpay = 15,              // Urpay (السعودية)
    AlRajhiTransfer = 16,    // تحويل مصرف الراجحي
    SNBTransfer = 17,        // تحويل البنك الأهلي SNB
    SamsungPay = 18,         // Samsung Pay
    PayBy = 19,              // PayBy (الإمارات)
    CareemPay = 20,          // Careem Pay (الإمارات)
    Knet = 21,               // كي نت KNET (الكويت)
    BoubyanPay = 22,         // Boubyan Pay (الكويت)
    Naps = 23,               // نابس NAPS (قطر)
    QPay = 24,               // كيو بي QPay (قطر)
    BenefitPay = 25,         // بنفت بي BenefitPay (البحرين)
    OmanNet = 26,            // عمان نت OmanNet (عمان)
    Thawani = 27,            // ثواني Thawani Pay (عمان)
    OrangeCash = 28,         // أورنج كاش (مصر)
    EtisalatCash = 29,       // اتصالات كاش / إي آند (مصر)
    Meeza = 30,              // بطاقة ميزة (مصر)
    Custom1 = 91,            // طريقة دفع مخصصة 1
    Custom2 = 92,            // طريقة دفع مخصصة 2
    Custom3 = 93             // طريقة دفع مخصصة 3
}

public enum SaleStatus
{
    Pending = 1,
    Completed = 2,
    Cancelled = 3,
    OnHold = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}

public enum OrderType
{
    DineIn = 1,
    Takeaway = 2,
    Delivery = 3
}
