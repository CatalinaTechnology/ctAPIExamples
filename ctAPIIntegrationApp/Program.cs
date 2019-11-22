using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ctAPIIntegrationApp
{
	class Program
	{
		private string _siteID;
		public string SiteID { get; set; }

		static void Main(string[] args)
		{
			ctAPIIntegrationApp.Program myProgram = new Program();
			myProgram.RunIt(args);
		}

		private void RunIt(string[] args)
		{
			{
				bool flag = false;
				bool importVoucher = false;
				bool showHelp = false;

				try
				{
					for (int i = 0; i < (int)args.Length; i++)
					{
						args[i] = args[i].Trim();
						if (args[i] != "" && args[i].StartsWith("-"))
						{
							if (args[i] == "-?")
							{
								showHelp = true;
								flag = true;
								throw new Exception("Show Help");
							}
							if (args[i].ToUpper() == "-SITE")
							{
								try
								{
									int num = i + 1;
									i = num;
									this.SiteID = args[num].Trim().ToUpper();
									i++;
								}
								catch
								{
									throw new Exception("You must pass a siteID.");
								}
							}
							if (args[i].ToUpper() == "-VOUCHER")
							{
								flag = true;
								importVoucher = true;
							}
						}
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (flag)
					{
						flag = false;
						if (!showHelp)
						{
							// TODO: do error handling here for commandline issues
						}
						Console.WriteLine("Usage:\nctAPIIntegrationApp -SITE <siteID> [-?, -VOUCHER]\nExample: if you are using siteID = TEST and you want to run vouchers, the usage is:\nctAPIIntegrationApp -SITE TEST -VOUCHER");
					}
					
				}
				if (flag)
				{
					if (importVoucher)
					{
						ImportVoucherToSL();
					}
				}
			}
		}

		private void ImportVoucherToSL()
		{
			var returnDataSet = GetDataForVoucher();

			foreach (System.Data.DataRow row in returnDataSet.Tables[0].Rows)
			{

				try
				{
					string batNbr = row["batNbr"].ToString();
					// you will now go through each row that was returned in the first table of the dataset

					ctDynamicsSL.VoucherAndAdjustmentEntry.screen voucherScreen;

					// going to assume that you have batNbr in your table.  if it is empty, then we will create a new batch.
					// If it has something in it, we will try to use that as the batch number
					if (string.IsNullOrEmpty(batNbr))
					{
						// if there is no batNbr, then we will just create an empty batch object and let ctAPI create the batNbr for us
						voucherScreen = VoucherService.getNewscreen(null);
						voucherScreen.myBatch.Crtd_Prog = Properties.Settings.Default.CrtdUpdProg;

						voucherScreen.myAPDoc.Crtd_Prog = Properties.Settings.Default.CrtdUpdProg;

						voucherScreen.myAPTran = new ctDynamicsSL.VoucherAndAdjustmentEntry.APTran[0];
					}
					else
					{
						// if a batNbr is coming in, we are going to try to use it
						voucherScreen = VoucherService.getScreenByBatNbr(batNbr);

						voucherScreen.myBatch.LUpd_Prog = Properties.Settings.Default.CrtdUpdProg;
						voucherScreen.myAPDoc = VoucherService.getNewAPDoc(null);
					}
					voucherScreen.myBatch.LUpd_Prog = Properties.Settings.Default.CrtdUpdProg;
					voucherScreen.myAPDoc.LUpd_Prog = Properties.Settings.Default.CrtdUpdProg;

					// create some notes.
					ctDynamicsSL.VoucherAndAdjustmentEntry.Snote batchNote = new ctDynamicsSL.VoucherAndAdjustmentEntry.Snote
					{
						notes = row["batchNote"].ToString()
					};
					batchNote.sNoteText = "";
					voucherScreen.batchNote = batchNote;

					voucherScreen.myBatch.CuryDrTot = 0;
					voucherScreen.myBatch.CuryCtrlTot = 0;

					voucherScreen.myBatch.CpnyID = row["cpnyID"].ToString().Trim();

					voucherScreen.myAPDoc.CpnyID = voucherScreen.myBatch.CpnyID;
					voucherScreen.myAPDoc.DocType = row["docType"].ToString();
					voucherScreen.myAPDoc.VendId = row["vendID"].ToString();
					voucherScreen.myAPDoc.InvcNbr = row["invoiceNumber"].ToString();
					voucherScreen.myAPDoc.InvcDate = (DateTime)row["invoiceDate"]; // NOTE:  I am assuming that this will be a DateTime datatype in the field from the DB  If not, you will have to make sure that the cast will work

					voucherScreen.myAPDoc.CuryOrigDocAmt = Math.Round((double)row["docAmount"], 2);
					voucherScreen.myAPDoc.OrigDocAmt = voucherScreen.myAPDoc.CuryOrigDocAmt;


					// now to do the transactions.  I dont know how you will save them.  So, I am just assuming that we will create a single transaction
					// if you have multiple transactions, you may want to use Table[1] (the second table) returned from the stored procedure or something
					List<ctDynamicsSL.VoucherAndAdjustmentEntry.APTran> apTrans = new List<ctDynamicsSL.VoucherAndAdjustmentEntry.APTran>();

					ctDynamicsSL.VoucherAndAdjustmentEntry.APTran tmpTran = VoucherService.getNewAPTran(null);
					tmpTran.Crtd_Prog = Properties.Settings.Default.CrtdUpdProg;
					tmpTran.LUpd_Prog = Properties.Settings.Default.CrtdUpdProg;

					tmpTran.TranDesc = row["TranDesc1"].ToString();
					tmpTran.ExtRefNbr = row["ExtRefNumber1"].ToString();

					double qty = 0;
					if (double.TryParse(row["Quantity1"].ToString(), out qty))
					{
						tmpTran.Qty = qty;
					}

					tmpTran.UnitDesc = "EA"; // lineItem.aptranUnitdesc;

					tmpTran.CuryUnitPrice = double.Parse(row["UnitPrice1"].ToString());
					tmpTran.UnitPrice = tmpTran.CuryUnitPrice;
					tmpTran.CuryTranAmt = double.Parse(row["TranAmount1"].ToString());
					tmpTran.Qty = tmpTran.CuryTranAmt / tmpTran.CuryUnitPrice;
					tmpTran.TranAmt = tmpTran.CuryTranAmt;



					tmpTran.CpnyID = row["TranCpnyID1"].ToString();
					tmpTran.Sub = row["TranSub1"].ToString();
					tmpTran.Acct = row["TranAcct1"].ToString();

					tmpTran.LineType = "N";
					tmpTran.DrCr = "D";


					tmpTran.TranAmt = tmpTran.CuryTranAmt;
					tmpTran.VendId = voucherScreen.myAPDoc.VendId;
					voucherScreen.myBatch.CuryDrTot += tmpTran.CuryTranAmt;
					voucherScreen.myAPDoc.CuryDocBal += tmpTran.CuryTranAmt;
					voucherScreen.myAPDoc.DocBal += tmpTran.CuryTranAmt;

					apTrans.Add(tmpTran);

					voucherScreen.myAPTran = apTrans.ToArray();

					voucherScreen.myBatch.CuryCtrlTot = voucherScreen.myBatch.CuryDrTot;
					voucherScreen.myBatch.CtrlTot = voucherScreen.myBatch.CuryCtrlTot;

					ctDynamicsSL.VoucherAndAdjustmentEntry.screen returnVoucher;

					// determine what type of action we will be doing (either an ADD for new batch or an UPDATE for existing batch)
					string actionType = string.IsNullOrEmpty(batNbr) ? "ADD" : "UPDATE";

					// call the API method to save the batch
					returnVoucher = VoucherService.editScreen(actionType, voucherScreen);

					if (returnVoucher.errorMessage.Trim() != "")
					{
						// an error occurred throw an exception to be handled.
						throw new Exception(returnVoucher.errorMessage);
					}

					// get the batNbr
					batNbr = returnVoucher.myBatch.BatNbr;

					// now lets update totals to make sure that all the batch totals are where we want them.
					var returnBatch = VoucherService.editBatch("UPDATETOTALS", returnVoucher.myBatch);
				}
				catch(Exception ex)
				{
					// todo:  put your error handling here
				}
			}
		}

		private System.Data.DataSet GetDataForVoucher()
		{
			List<ctDynamicsSL.Common.nameValuePairs> parms = new List<ctDynamicsSL.Common.nameValuePairs>
			{
				new ctDynamicsSL.Common.nameValuePairs{name = "Param1", value = "Put Value Here" },
				new ctDynamicsSL.Common.nameValuePairs{name = "Param2", value = "Put Value Here" },

			};

			// this is the stored procedure name that we will be calling.  I have this in the project 
			// in the DatabaseScripts folder in a file called StoredProcedures.sql
			string sqlProcName = "xct_CustomProc";
			string sqlHash = ctStandardLib.ctHelper.getHash(Properties.Settings.Default.SITEKEY, sqlProcName);
			var returnVal = CommonService.customSQLCall(sqlProcName, parms.ToArray(), sqlHash);

			return returnVal;
		}

		private ctDynamicsSL.VoucherAndAdjustmentEntry.voucherAndAdjustmentEntry _voucherService = null;
		private ctDynamicsSL.VoucherAndAdjustmentEntry.voucherAndAdjustmentEntry VoucherService
		{
			get
			{
				if (this._voucherService == null)
				{
					this._voucherService = new ctDynamicsSL.VoucherAndAdjustmentEntry.voucherAndAdjustmentEntry
					{
						Timeout = 300000,
						ctDynamicsSLHeaderValue = new ctDynamicsSL.VoucherAndAdjustmentEntry.ctDynamicsSLHeader
						{
							siteID = Properties.Settings.Default.SITEID,
							cpnyID = Properties.Settings.Default.CPNYID,
							licenseKey = Properties.Settings.Default.LICENSEKEY,
							licenseName = Properties.Settings.Default.LICENSENAME,
							licenseExpiration = Properties.Settings.Default.LICENSEEXPIRATION,
							siteKey = Properties.Settings.Default.SITEKEY,
							softwareName = Properties.Settings.Default.SOFTWARENAME
						}
					};
				}
				return this._voucherService;
			}
			set
			{
				this._voucherService = value;
			}
		}
		private ctDynamicsSL.Common.common _commonService = null;
		private ctDynamicsSL.Common.common CommonService
		{
			get
			{
				if (this._commonService == null)
				{
					this._commonService = new ctDynamicsSL.Common.common
					{
						Timeout = 300000,
						ctDynamicsSLHeaderValue = new ctDynamicsSL.Common.ctDynamicsSLHeader
						{
							siteID = SiteID,
							cpnyID = Properties.Settings.Default.CPNYID,
							licenseKey = Properties.Settings.Default.LICENSEKEY,
							licenseName = Properties.Settings.Default.LICENSENAME,
							licenseExpiration = Properties.Settings.Default.LICENSEEXPIRATION,
							siteKey = Properties.Settings.Default.SITEKEY,
							softwareName = Properties.Settings.Default.SOFTWARENAME
						}
					};
				}
				return this._commonService;
			}
			set
			{
				this._commonService = value;
			}
		}
	}
}
