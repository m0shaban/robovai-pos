using System;
using System.Globalization;
using System.Windows.Data;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Converters;

public class PaymentMethodToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PaymentMethod pm)
        {
            return pm switch
            {
                PaymentMethod.Cash => "كاش",
                PaymentMethod.Mada => "مدى (Mada)",
                PaymentMethod.Card => "بطاقة / فيزا",
                PaymentMethod.StcPay => "STC Pay",
                PaymentMethod.ApplePay => "Apple Pay",
                PaymentMethod.Urpay => "Urpay",
                PaymentMethod.AlRajhiTransfer => "تحويل الراجحي",
                PaymentMethod.SNBTransfer => "تحويل الأهلي SNB",
                PaymentMethod.SamsungPay => "Samsung Pay",
                PaymentMethod.PayBy => "PayBy (الإمارات)",
                PaymentMethod.CareemPay => "Careem Pay",
                PaymentMethod.Knet => "كي نت KNET",
                PaymentMethod.BoubyanPay => "Boubyan Pay",
                PaymentMethod.Naps => "نابس NAPS",
                PaymentMethod.QPay => "كيو بي QPay",
                PaymentMethod.BenefitPay => "بنفت بي BenefitPay",
                PaymentMethod.OmanNet => "عمان نت OmanNet",
                PaymentMethod.Thawani => "ثواني Thawani Pay",
                PaymentMethod.Tamara => "تمارا (Tamara)",
                PaymentMethod.Tabby => "تابي (Tabby)",
                PaymentMethod.VodafoneCash => "فودافون كاش",
                PaymentMethod.InstaPay => "انستا باي",
                PaymentMethod.OrangeCash => "أورنج كاش",
                PaymentMethod.EtisalatCash => "اتصالات كاش",
                PaymentMethod.Meeza => "ميزة (Meeza)",
                PaymentMethod.BankTransfer => "تحويل بنكي",
                PaymentMethod.MobileMoney => "محفظة إلكترونية",
                PaymentMethod.Split => "دفع مقسم",
                PaymentMethod.Deferred => "آجل",
                PaymentMethod.StaffMeal => "وجبة ضيافة",
                PaymentMethod.Custom1 => "طريقة مخصصة 1",
                PaymentMethod.Custom2 => "طريقة مخصصة 2",
                PaymentMethod.Custom3 => "طريقة مخصصة 3",
                _ => pm.ToString()
            };
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
