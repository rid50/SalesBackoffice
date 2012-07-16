using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SalesBackOffice.Models
{
    public class PendingTransactionRepository : BaseDataContext, IPendingTransactionRepository
    {

        #region IPendingTransactionRepository Members


        public IEnumerable<StoreReceiptVoucherQuantityGroupByResult> GetStoreReceiptVoucherQuantityGroupBy()      // Store Procedure
        {
            try
            {
                return SalesBackOffice.StoreReceiptVoucherQuantityGroupBy().AsEnumerable();
            }
            catch { return null; }
            //catch (Exception ex) { var e = ex.ToString(); return null; }
        }

        public IEnumerable<DeliveryNotesQuantityGroupByResult> GetDeliveryNotesQuantityGroupBy()      // Store Procedure
        {
            try
            {
                return SalesBackOffice.DeliveryNotesQuantityGroupBy().AsEnumerable();
            }
            catch { return null; }
        }

        public StoreReceiptVoucher Details(int voucher_id)
        {
            try
            {
                return SalesBackOffice.StoreReceiptVouchers.Single(c => c.voucher_id == voucher_id);
            }
            catch { return null; }
        }


        public IEnumerable<StoreReceiptVoucher> GetStoreReceiptVouchers()
        {
            try
            {
                var srvs = SalesBackOffice.StoreReceiptVouchers;
                return srvs.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<PurchaseRequisition> GetPurchaseRequisitions()
        {
            try
            {
                return SalesBackOffice.PurchaseRequisitions.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<Contract> GetContracts()
        {
            try
            {
                return SalesBackOffice.Contracts.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<ContractDetail> GetContractDetails()
        {
            try
            {
                return SalesBackOffice.ContractDetails.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<DeliveryNote> GetDeliveryNotes()
        {
            try
            {
                return SalesBackOffice.DeliveryNotes.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<Customer> GetCustomers()
        {
            try
            {
                return SalesBackOffice.Customers.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<Supplier> GetSuppliers()
        {
            try
            {
                return SalesBackOffice.Suppliers.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<part_list> GetParts()
        {
            try
            {
                SalesBackOffice.DeferredLoadingEnabled = false;
                return SalesBackOffice.part_lists.AsEnumerable();
            }
            catch { return null; }
        }

        public IEnumerable<invoice_detail> GetInvoiceDetails()
        {
            try
            {
                return SalesBackOffice.invoice_details.AsEnumerable();
            }
            catch { return null; }
        }

        #endregion
    }

    public interface IPendingTransactionRepository
    {
        StoreReceiptVoucher Details(int voucher_id);
        IEnumerable<StoreReceiptVoucher> GetStoreReceiptVouchers();
        IEnumerable<PurchaseRequisition> GetPurchaseRequisitions();
        IEnumerable<ContractDetail> GetContractDetails();
    }
}
