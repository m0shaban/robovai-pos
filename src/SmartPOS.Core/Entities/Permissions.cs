using System;

namespace SmartPOS.Core.Entities;

[Flags]
public enum Permissions : long
{
    None = 0,
    
    // Application Features
    ViewDashboard = 1L << 0,
    AccessPOS = 1L << 1,
    ManageProducts = 1L << 2,
    ManageCategories = 1L << 3,
    ManageSuppliers = 1L << 4,
    ManageCustomers = 1L << 5,
    ViewReports = 1L << 6,
    ViewProfit = 1L << 7,
    ManageUsers = 1L << 8,
    ManageSettings = 1L << 9,
    ManageExpenses = 1L << 10,
    ManagePurchases = 1L << 11,
    ManageReturns = 1L << 12,
    ManageShifts = 1L << 13,

    // POS Restrictions & Overrides
    OpenCashDrawer = 1L << 20,
    ApplyDiscount = 1L << 21,
    ApplyHighDiscount = 1L << 22, // Requires explicit permission or PIN
    VoidItem = 1L << 23,          // Delete from cart after scan
    HoldSale = 1L << 24,
    IssueRefund = 1L << 25,

    // Security
    ProvideAdminPin = 1L << 60,   // Allows this user's PIN to bypass restrictions
    All = long.MaxValue           // SuperAdmin implicit
}
