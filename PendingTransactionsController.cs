using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using SalesBackOffice.Models;
using jqMvcGrid.Models;
using SalesBackOffice.Helpers;

namespace SalesBackOffice.Controllers
{
    public class PendingTransactionsController : Controller
    {
        private const string STORE_NAME = "ITS store";

    //    SalesBackOfficeDataContext salesDb = new SalesBackOfficeDataContext();
        private IPendingTransactionRepository _repository = new PendingTransactionRepository();


        //
        // GET: /PendingTransactions/

        public ActionResult Index()
        {
            var repository = new PendingTransactionRepository();
/*
            //IEnumerable<PendingTransactionDto> srv_quantity = from srv in repository.GetStoreReceiptVouchers()
            var srv_quantity = from srv in repository.GetStoreReceiptVouchers()
                        from srv_detail in srv.StoreReceiptVoucherDetails
                        group srv_detail by new { srv.contract_id, srv_detail.supplier_id, srv_detail.part_no } into g
                            select new PendingTransactionDto
                            {
                                contract_id = g.Key.contract_id,
                                supplier_id = g.Key.supplier_id,
                                part_no = g.Key.part_no,
                                sum_quantity = g.Sum(s => s.quantity)
                            };

            srv_quantity.Count();
            return View(srv_quantity.ToList());
*/
            var srv_quantity = repository.GetStoreReceiptVoucherQuantityGroupBy();  // Store Procedure

            //srv_quantity.Count();
            //return View(srv_quantity.ToList());

            var diff_pr_srv = from pr in repository.GetPurchaseRequisitions()
                              join cd in repository.GetContractDetails()
                              on new { pr.contract_id, pr.supplier_id } equals new { cd.contract_id, cd.supplier_id }
                              join srv in srv_quantity
                              on new { cd.contract_id, cd.supplier_id, cd.part_no } equals new { srv.contract_id, srv.supplier_id, srv.part_no } into left_join
                              from srv in left_join.DefaultIfEmpty()
                              where (srv == null ? cd.quantity : cd.quantity - srv.SumQ) != 0 && STORE_NAME != pr.supplier_id
                              //where (srv == null ? cd.quantity : cd.quantity - srv.sum_quantity) != 0 && STORE_NAME != pr.supplier_id
                              select new PendingTransactionDto
                              {
                                  contract_id = cd.contract_id,
                                  supplier_id = cd.supplier_id
                                  //part_no = cd.part_no,
                                  //sum_quantity = srv == null ? cd.quantity : cd.quantity - srv.sum_quantity
                              };
/*
            var dn_quantity = from dn in repository.GetDeliveryNotes()
                               from dn_detail in dn.DeliveryNoteDetails
                               group dn_detail by new { dn.contract_id, dn_detail.supplier_id, dn_detail.part_no } into g
                               select new PendingTransactionDto
                               {
                                   contract_id = g.Key.contract_id,
                                   supplier_id = g.Key.supplier_id,
                                   part_no = g.Key.part_no,
                                   sum_quantity = g.Sum(s => s.quantity)
                               };
*/
            var dn_quantity = repository.GetDeliveryNotesQuantityGroupBy();  // Store Procedure

            var diff_pr_dn = from pr in repository.GetPurchaseRequisitions()
                              join cd in repository.GetContractDetails()
                              on new { pr.contract_id, pr.supplier_id } equals new { cd.contract_id, cd.supplier_id }
                              join dn in dn_quantity
                              on new { cd.contract_id, cd.supplier_id, cd.part_no } equals new { dn.contract_id, dn.supplier_id, dn.part_no } into left_join
                              from dn in left_join.DefaultIfEmpty()
                              where (dn == null ? cd.quantity : cd.quantity - dn.SumQ) != 0 && STORE_NAME == pr.supplier_id
                              //where (dn == null ? cd.quantity : cd.quantity - dn.sum_quantity) != 0 && STORE_NAME == pr.supplier_id
                              select new PendingTransactionDto
                              {
                                  contract_id = cd.contract_id,
                                  supplier_id = cd.supplier_id
                                  //part_no = cd.part_no,
                                  //sum_quantity = dn == null ? cd.quantity : cd.quantity - dn.sum_quantity
                              };

            var union_pr_srv_dn = diff_pr_srv.Union(diff_pr_dn);

            var pendingPRs = from customer in repository.GetCustomers()
                                from contract in customer.Contracts
                                join pr in repository.GetPurchaseRequisitions()
                                on contract.contract_id equals pr.contract_id
                                join un in union_pr_srv_dn
                                on new { pr.contract_id, pr.supplier_id } equals new { un.contract_id, un.supplier_id }
                                orderby pr.date_entry, pr.contract_id
                                where (pr.lead_time == null ? pr.date_entry : pr.date_entry.AddDays(Convert.ToDouble(pr.lead_time))) < DateTime.Now.AddDays(-1d)
                                select new PendingTransactionDto
                                {
                                    CustomerName = customer.descr,
                                    DateEntry = pr.date_entry.ToString("dd/MM/yyy"),
                                    contract_id = pr.contract_id,
                                    supplier_id = pr.supplier_id,
                                    OurRef = pr.our_ref,
                                    LeadTime = pr.lead_time,
                                };

            var pendingPRs_distinct = pendingPRs.GroupBy(x => new { x.contract_id, x.supplier_id }).Select(x => x.First());

            var pendingPRs_final = from pr in pendingPRs_distinct
                                   join supplier in repository.GetSuppliers()
                                   on pr.supplier_id equals supplier.code
                                   select new PendingTransactionDto
                                   {
                                        contract_id = pr.contract_id,
                                        CustomerName = pr.CustomerName,
                                        DateEntry = pr.DateEntry,
                                        //supplier_id = pr.supplier_id,
                                        //OurRef = pr.OurRef,
                                        LeadTime = pr.LeadTime,
                                        SupplierName = supplier.descr
                                   };


//            @"SELECT DISTINCT customer_list.descr AS [Customer Name], supplier_list.descr AS [Supplier Name], pr.date_entry AS [Date], 
//                              contracts.contract_id AS [Contract ID], contracts.supplier_id AS [Supplier ID], pr.our_ref AS [Our Ref],
//                              pr.lead_time AS [Lead Time]
//              FROM    (
//                          (
//                              customer_list INNER JOIN contract_list ON customer_list.code = contract_list.customer_id
//                          )
//                          INNER JOIN 
//                          (
//                              supplier_list INNER JOIN contracts ON supplier_list.code = contracts.supplier_id
//                          )
//                          ON contract_list.contract_id = contracts.contract_id
//                      ) 
//                      INNER JOIN 
//                      (
//                          q_Items_For_Pending_PR INNER JOIN pr 
//                          ON (q_Items_For_Pending_PR.supplier_id = pr.supplier_id) AND (q_Items_For_Pending_PR.contract_id = pr.contract_id)
//                      )
//                      ON (contracts.supplier_id = pr.supplier_id) AND (contracts.contract_id = pr.contract_id)
//              WHERE ((([pr].[date_entry]+IIf(IsNull([lead_time]),0,[lead_time]))<Now()-1))
//              ORDER BY pr.date_entry, contracts.contract_id;";




            //.Select( x => new { PendingTransactionId = x.voucher_id, PendingTransactionCount = x.supplier_id });
             //return View("ViewName", someLinq.Select(new { x=1, y=2}.ToExpando());
            //return View(model.ToExpando());

//            return View();
            return View(pendingPRs_final.ToList());

        }

        public ActionResult GetAllPendingPRsTest()
        {
            return GetAllPendingPRs(1, 10, null, null, null);
        }

        public ActionResult GetAllPendingPRs(int page, int rows, string search, string sidx, string sord)
        {
            var repository = new PendingTransactionRepository();
            var srv_quantity = repository.GetStoreReceiptVoucherQuantityGroupBy();  // Store Procedure

            var diff_pr_srv = from pr in repository.GetPurchaseRequisitions()
                              join cd in repository.GetContractDetails()
                              on new { pr.contract_id, pr.supplier_id } equals new { cd.contract_id, cd.supplier_id }
                              join srv in srv_quantity
                              on new { cd.contract_id, cd.supplier_id, cd.part_no } equals new { srv.contract_id, srv.supplier_id, srv.part_no } into left_join
                              from srv in left_join.DefaultIfEmpty()
                              where (srv == null ? cd.quantity : cd.quantity - srv.SumQ) != 0 && STORE_NAME != pr.supplier_id
                              //where (srv == null ? cd.quantity : cd.quantity - srv.sum_quantity) != 0 && STORE_NAME != pr.supplier_id
                              select new PendingTransactionDto
                              {
                                  contract_id = cd.contract_id,
                                  supplier_id = cd.supplier_id
                                  //part_no = cd.part_no,
                                  //sum_quantity = srv == null ? cd.quantity : cd.quantity - srv.sum_quantity
                              };
            var dn_quantity = repository.GetDeliveryNotesQuantityGroupBy();  // Store Procedure

            var diff_pr_dn = from pr in repository.GetPurchaseRequisitions()
                             join cd in repository.GetContractDetails()
                             on new { pr.contract_id, pr.supplier_id } equals new { cd.contract_id, cd.supplier_id }
                             join dn in dn_quantity
                             on new { cd.contract_id, cd.supplier_id, cd.part_no } equals new { dn.contract_id, dn.supplier_id, dn.part_no } into left_join
                             from dn in left_join.DefaultIfEmpty()
                             where (dn == null ? cd.quantity : cd.quantity - dn.SumQ) != 0 && STORE_NAME == pr.supplier_id
                             //where (dn == null ? cd.quantity : cd.quantity - dn.sum_quantity) != 0 && STORE_NAME == pr.supplier_id
                             select new PendingTransactionDto
                             {
                                 contract_id = cd.contract_id,
                                 supplier_id = cd.supplier_id
                                 //part_no = cd.part_no,
                                 //sum_quantity = dn == null ? cd.quantity : cd.quantity - dn.sum_quantity
                             };

            var union_pr_srv_dn = diff_pr_srv.Union(diff_pr_dn);

            var pendingPRs = from customer in repository.GetCustomers()
                             from contract in customer.Contracts
                             join pr in repository.GetPurchaseRequisitions()
                             on contract.contract_id equals pr.contract_id
                             join un in union_pr_srv_dn
                             on new { pr.contract_id, pr.supplier_id } equals new { un.contract_id, un.supplier_id }
                             orderby pr.date_entry, pr.contract_id
                             where (pr.lead_time == null ? pr.date_entry : pr.date_entry.AddDays(Convert.ToDouble(pr.lead_time))) < DateTime.Now.AddDays(-1d)
                             select new PendingTransactionDto
                             {
                                 CustomerName = customer.descr,
                                 DateEntry = pr.date_entry.ToString("dd/MM/yyy"),
                                 contract_id = pr.contract_id,
                                 supplier_id = pr.supplier_id,
                                 OurRef = pr.our_ref,
                                 LeadTime = pr.lead_time,
                             };

            var pendingPRs_distinct = pendingPRs.GroupBy(x => new { x.contract_id, x.supplier_id }).Select(x => x.First());

            var pendingPRs_final = (from pr in pendingPRs_distinct
                                   join supplier in repository.GetSuppliers()
                                   on pr.supplier_id equals supplier.code
                                   select new
                                   {
                                       contract_id = pr.contract_id,
                                       CustomerName = pr.CustomerName,
                                       DateEntry = pr.DateEntry,
                                       //supplier_id = pr.supplier_id,
                                       //OurRef = pr.OurRef,
                                       LeadTime = pr.LeadTime,
                                       SupplierName = supplier.descr
                                   }).ToArray();

            int pageIndex = Convert.ToInt32(page) - 1;
            int pageSize = rows;
            int totalRecords = pendingPRs_final.Count();
            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);

            var jsonData = new
            {
                Total = totalPages,
                Page = page,
                Records = totalRecords,
                Rows = pendingPRs_final
            };

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

/*
        public ActionResult List(int page, int rows, string search, string sidx, string sord)
        {
            var repository = new PendingTransactionRepository();
            var model = from entity in repository.GetStoreReceiptVouchers().OrderBy(sidx + " " + sord)
                        select new
                        {
                            PendingTransactionId = entity.voucher_id,
                            PendingTransactionName = entity.supplier_id,
                        };
            //var ret = Json(model.ToJqGridData(page, rows, null, search,
              //new[] { "CategoryName", "Description" }), JsonRequestBehavior.AllowGet);

            //return ret;
            return Json(model.ToJqGridData(page, rows, null, search,
                new[] { "PendingTransactionId", "PendingTransactionName" }), JsonRequestBehavior.AllowGet);
        }
*/

        //
        // GET: /PendingTransactions/PendingPRDetails/5

        public ActionResult PendingPRDetailsTest(string contract_id)
        {
            return PendingPRDetails(1, 10, null, null, null, contract_id);
        }

        [JsonpFilter]
        public ActionResult PendingPRDetails(int page, int rows, string search, string sidx, string sord, string contract_id)
        {
            if (contract_id == "")
            {
                var jsonEmptyData = new
                {
                    Total = 0,
                    Page = 1,
                    Records = 0,
                    Rows = new { item_id = 0, part_no = "", part_no_description = "", sum_quantity = 0, unit_price = 0d, total_price = 0d }
                };
                return Json(jsonEmptyData, JsonRequestBehavior.AllowGet);
            }

            var repository = new PendingTransactionRepository();

/*
            //IEnumerable<PendingTransactionDto> srv_quantity = from srv in repository.GetStoreReceiptVouchers()
            var srv_quantity = from srv in repository.GetStoreReceiptVouchers()
                               from srv_detail in srv.StoreReceiptVoucherDetails
                               group srv_detail by new { srv.contract_id, srv_detail.supplier_id, srv_detail.part_no } into g
                               select new PendingTransactionDto
                               {
                                   contract_id = g.Key.contract_id,
                                   supplier_id = g.Key.supplier_id,
                                   part_no = g.Key.part_no,
                                   sum_quantity = g.Sum(s => s.quantity)
                               };

            //srv_quantity.Count();
            //return View(srv_quantity.ToList());
*/
            var srv_quantity = repository.GetStoreReceiptVoucherQuantityGroupBy();  // Store Procedure

            var diff_pr_srv = from pr in repository.GetPurchaseRequisitions()
                              join cd in repository.GetContractDetails()
                              on new { pr.contract_id, pr.supplier_id } equals new { cd.contract_id, cd.supplier_id }
                              join srv in srv_quantity
                              on new { cd.contract_id, cd.supplier_id, cd.part_no } equals new { srv.contract_id, srv.supplier_id, srv.part_no } into left_join
                              from srv in left_join.DefaultIfEmpty()
                              where (srv == null ? cd.quantity : cd.quantity - srv.SumQ) != 0 && STORE_NAME != pr.supplier_id
                              //where (srv == null ? cd.quantity : cd.quantity - srv.sum_quantity) != 0 && STORE_NAME != pr.supplier_id
                              select new PendingTransactionDto
                              {
                                  contract_id = cd.contract_id,
                                  supplier_id = cd.supplier_id,
                                  part_no = cd.part_no,
                                  sum_quantity = srv == null ? cd.quantity : cd.quantity - (int)srv.SumQ
                                  //sum_quantity = srv == null ? cd.quantity : cd.quantity - srv.sum_quantity
                              };

							  var dn_quantity = repository.GetDeliveryNotesQuantityGroupBy();  // Store Procedure

            var diff_pr_dn = from pr in repository.GetPurchaseRequisitions()
                             join cd in repository.GetContractDetails()
                             on new { pr.contract_id, pr.supplier_id } equals new { cd.contract_id, cd.supplier_id }
                             join dn in dn_quantity
                             on new { cd.contract_id, cd.supplier_id, cd.part_no } equals new { dn.contract_id, dn.supplier_id, dn.part_no } into left_join
                             from dn in left_join.DefaultIfEmpty()
                             where (dn == null ? cd.quantity : cd.quantity - dn.SumQ) != 0 && STORE_NAME == pr.supplier_id
                             //where (dn == null ? cd.quantity : cd.quantity - dn.sum_quantity) != 0 && STORE_NAME == pr.supplier_id
                             select new PendingTransactionDto
                             {
                                 contract_id = cd.contract_id,
                                 supplier_id = cd.supplier_id,
                                 part_no = cd.part_no,
                                 sum_quantity = dn == null ? cd.quantity : cd.quantity - (int)dn.SumQ
                                 //sum_quantity = dn == null ? cd.quantity : cd.quantity - dn.sum_quantity
                             };

            var union_pr_srv_dn = diff_pr_srv.Union(diff_pr_dn);

            var pendingPRDetails = from contract in repository.GetContracts()
                             join contractDetail in repository.GetContractDetails()
                             on contract.contract_id equals contractDetail.contract_id
                             join un in union_pr_srv_dn
                             on new {contractDetail.contract_id, contractDetail.supplier_id, contractDetail.part_no } equals new { un.contract_id, un.supplier_id, un.part_no }
                             where (contract.contract_id == contract_id)
                             select new PendingTransactionDto
                             {
                                contract_id = contractDetail.contract_id,
                                supplier_id = contractDetail.supplier_id,
                                item_id = contractDetail.item_id,
                                part_no = contractDetail.part_no,
                                sum_quantity = un.sum_quantity
                                //unit_price = contractDetail.unit_price
                                //total_price = contractDetail.cost_price
                             };

            //var parts = repository.GetParts();
            //IEnumerable<part_list> parts = repository.GetParts();
            //IEnumerable<PendingTransactionDto> pendingPRDetails_distinct = pendingPRDetails.GroupBy(x => new { x.contract_id, x.supplier_id, x.part_no }).Select(x => x.First());

            var pendingPRDetails_distinct = pendingPRDetails.GroupBy(x => new { x.contract_id, x.supplier_id, x.part_no }).Select(x => x.First());

/*
            var questions = context.Questions
              .OrderBy(sidx + " " + sord)
              .Skip(pageIndex * pageSize)
              .Take(pageSize);
*/
             var rows2 = (
                  from part in repository.GetParts()
                  join pr in pendingPRDetails_distinct
                  on new { part.supplier_id, part.code } equals new { pr.supplier_id, code = pr.part_no }
                  //on true equals true
                  //where (part.supplier_id == pr.supplier_id && part.code == pr.part_no)
                  orderby pr.item_id ascending
                  //select new PendingTransactionDto
                  select new
                  {
                        item_id = pr.item_id,
                        part_no = pr.part_no,
                        part_no_description = part.descr,
                        sum_quantity = pr.sum_quantity,
                        unit_price = (part.unit_price == 0) ? part.cost_price : part.unit_price,
                        total_price = ((part.unit_price == 0) ? part.cost_price : part.unit_price) * pr.sum_quantity

                  }).ToArray();

             int pageIndex = Convert.ToInt32(page) - 1;
             int pageSize = rows;
             int totalRecords = rows2.Count();
             int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);

             var jsonData = new
             {
                 Total = totalPages,
                 Page = page,
                 Records = totalRecords,
                 Rows = rows2
             };

            //if (sidx == null && sord == null)
                //return View((pendingPRDetails_final).ToList());
                //return View(((IEnumerable<PendingTransactionDto>)pendingPRDetails_final).ToList());
            //else
             //String callBack = Request.Params.Get("callback");
             //if (String.IsNullOrEmpty(callBack))
                //return Json(jsonData, JsonRequestBehavior.AllowGet);
                return new JsonResult
                {
                    Data = jsonData,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            //else
                //return callBack + "(" + Json(jsonData, JsonRequestBehavior.AllowGet) + ")";
                //return Json(((IQueryable<PurchaseRequisitionDto>)pendingPRDetails_final).ToJqGridData(page, rows, null, search, null), JsonRequestBehavior.AllowGet);
        }
    }
}
