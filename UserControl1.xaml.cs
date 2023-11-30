using Data;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SMTChangeDisplay
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControl1 : Component.Controls.User.UserVendor
    {
        DataView vi;
        string partcode = "";
        Boolean CanUpdate = false;
        Boolean AutoFresh = false;
        Thread thread;
        int moid = -1;
        int lineid = -1;
        string zhanwei = "";
        string tempbom = "";
        DataTable dt = null;
        public UserControl1(Framework.SystemAuthority authority) :
            base(authority)
        {
            InitializeComponent();
            
        }

        public void Import()
        {
            DataView view = vi;
            if (view==null||view.Count < 1)
            {
                Component.MessageBox.MyMessageBox.ShowError("没有获取到MAC数据！");
                return;
            }
            List<Dictionary<string, object>> l = new List<Dictionary<string, object>>();
            int rowNo = 0;
            foreach(DataRowView row in view)
            {
                rowNo++;
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("rowNo", rowNo);
                foreach (DataColumn col in view.Table.Columns)
                {
                    if(!dictionary.ContainsKey(col.ColumnName.ToUpper().Trim()))
                    {
                        dictionary.Add(col.ColumnName.ToUpper().Trim(), row[col.ColumnName]);
                    }
                }
                l.Add(dictionary);
            }
            Dictionary<string, List<Dictionary<string, object>>> items = new Dictionary<string, List<Dictionary<string, object>>>();
            items.Add("LOTSN", l);
            string xml = WinAPI.File.XMLHelper.CreateXML(null, items, null);
            Parameters parameters = new Parameters()
                .Add("UserId", Framework.App.User.UserId)
                .Add("LineId",Framework.App.Resource.LineId)
                .Add("ResourceId",Framework.App.Resource.ResourceId)
                .Add("ShiftTypeId",Framework.App.Resource.ShiftTypeId)
                .Add("PluginId", PluginId)
                .Add("xml", xml, SqlDbType.Xml, int.MaxValue)
                .Add("ToWorkStationCode",tbTOOP.Text)
                .Add("Return_Message", DBNull.Value, SqlDbType.NVarChar, int.MaxValue, ParameterDirection.Output)
                .Add("return_value", DBNull.Value, SqlDbType.Int, 50, ParameterDirection.ReturnValue);

            int handle = Component.MaskBusy.Busy(root, "正在保存数据...");
            System.Threading.Tasks.Task<Result<Parameters>>.Factory.StartNew(() =>
            {
                Result<Parameters> result=new Result<Parameters>();
                result.HasError=false;
                try
                {
                    parameters = DB.DBHelper.ExecuteParameters("Pro_Inp_WorkStation_Return", parameters, ExecuteType.StoredProcedure);
                }
                catch (Exception ex)
                {
                    result.HasError = true;
                    result.Message = ex.Message;
                }
                result.Value = parameters;
                return result;
            }).ContinueWith(r =>
            {
                if(r.Result.HasError)
                {
                    MessageBox.Show(r.Result.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (int.Parse(r.Result.Value["return_value"].ToString()) != 1)
                {
                    // MessageBox.Show(r.Result.Value["Return_Message"].ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Color color= Color.FromRgb(255, 0, 0);
                    tital.Foreground = new SolidColorBrush(color);
                    tital.Text = r.Result.Value["Return_Message"].ToString();
                }
                else
                {
                    // MessageBox.Show(r.Result.Value["Return_Message"].ToString(), "提示", MessageBoxButton.OK, MessageBoxImage.Question);
                    Color color = Color.FromRgb(0,255, 0);
                    tital.Foreground = new SolidColorBrush(color);
                    tital.Text = r.Result.Value["Return_Message"].ToString();
                    dataGrid.Value = null;
                }
                Component.MaskBusy.Hide(root, handle);
            }, Framework.App.Scheduler);
        }
       

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Component.Windows.AuthorityLogin login = new Component.Windows.AuthorityLogin(MenuId, '4');
            login.Owner = Component.App.Portal;
            Import();
            //if ((login.ShowDialog().Value) && (login.UserId == 1020))
            //{
            //    Import();
            //}
            //else
            //{
            //    MessageBox.Show("您不是超级管理员，无法完成您指定的操作","操作失败",MessageBoxButton.OK);
            //}
        }

        private void dataGridQuery_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sql;
            Parameters parameters;
            if (barcode.Text != string.Empty)
            {
                
                    sql = @"SELECT  Bas_MO.MOCode,
				        Bas_MO_Mac.Mac,
				        Inp_Lot.OPId,
				        Bas_OP.OPDesc
				        FROM Bas_MO_Mac
				        LEFT JOIN Bas_MO on Bas_MO.MOId=Bas_MO_Mac.MOId
				        LEFT JOIN Inp_Lot ON Inp_Lot.LotSN=Bas_MO_Mac.mac
				        LEFT JOIN Bas_OP ON  Bas_OP.OPId=Inp_Lot.OPId
				        WHERE Bas_MO_Mac.Mac='" + barcode.Text + "' OR Bas_MO_Mac.DSN='" + barcode.Text + "' OR Bas_MO_Mac.DeviceSerialNumber='" + barcode.Text + "' OR Bas_MO_Mac.STBNO='" + barcode.Text + "'";
                    parameters = new Parameters();
                
                
            }
            else if (tbMO.Text!=string.Empty && tbOP.Text!=string.Empty)
            {
                sql = @" SELECT BAS_MO.MOCode,
                Bas_MO_Mac.Mac,
				Inp_Lot.OPId,
				Bas_OP.OPDesc
                FROM BAS_MO
                LEFT JOIN Inp_Lot ON Inp_Lot.MOId = BAS_MO.MOId
                LEFT JOIN Bas_MO_Mac ON Bas_MO_Mac.LotId = Inp_Lot.LotId
                LEFT JOIN Bas_OP ON Bas_OP.OPId = Inp_Lot.OPId
                WHERE BAS_MO.MOCode = @MOCode and Bas_OP.OPCode = @OPCode";
                parameters = new Parameters()
                .Add("MOCode", tbMO.Text, SqlDbType.NVarChar, 50)
                .Add("OPCode", tbOP.Text, SqlDbType.NVarChar, 50);
            }else
            {
                MessageBox.Show("订单号或站位不能为空!!!");
                return;
            }

            int handle = Component.MaskBusy.Busy(root, "正在查询数据...");
            System.Threading.Tasks.Task<Result<DataTable>>.Factory.StartNew(() =>
            {
                Result<DataTable> result = new Result<DataTable>() { HasError = false };
                DataTable dt = null;
                try
                {
                    dt = DB.DBHelper.GetDataTable(sql, parameters, ExecuteType.Text);
                }
                catch (Exception ex)
                {
                    result.HasError = true;
                    result.Message = ex.Message;
                }
                result.Value = dt;
                return result;

            }).ContinueWith(r =>
            {
                if (r.Result.HasError)
                {
                    MessageBox.Show(r.Result.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    vi= (r.Result.Value).DefaultView;
                    dataGridQuery1.ItemsSource = vi;
                }
                Component.MaskBusy.Hide(root, handle);
            }, Framework.App.Scheduler);
        }

        //private void Control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{

        //}

        private void Barcode_KeyDown(object sender, KeyEventArgs e)
        {
            
            
            if (e.Key==Key.Enter)
            {
                partcode = barcode.Text.Trim();
                //if (partcode == string.Empty)
                //{
                //    MessageBox.Show("按料号查询时，料号不能为空！！！");
                //    return;
                //}
                //if(moid==-1||lineid==-1)
                //{
                //    MessageBox.Show("请先选择工单号及产线！！！");
                //    return;
                //}
                if (tbMO.Text==string.Empty)
                {
                    moid = -1;
                }
                if(tbOP.Text==string.Empty)
                {
                    lineid = -1;
                }
                //string sql;
                Parameters parameters;
                parameters = new Parameters()
                .Add("BomName",bomname.Text, SqlDbType.NVarChar, 1024)
                .Add("MOId", moid, SqlDbType.Int, 50)
                .Add("LineId", lineid, SqlDbType.Int, 50)
                .Add("PartCode", partcode, SqlDbType.NVarChar, 70);
                
                Result<Parameters, DataSet> result = null;
                try
                {
                    result = DB.DBHelper.ExecuteParametersSource("Get_SMT_Material_Report", parameters, ExecuteType.StoredProcedure);
                    dt = result.Value2.Tables[0];
                    grid1.ItemsSource = dt.DefaultView;
                    double ScreenWith = SystemParameters.WorkArea.Width;// 800;// this.Width;// grid1.Width;
                    for (int i = 0; i < grid1.Columns.Count; i++)
                    {
                        grid1.Columns[i].Width = (ScreenWith / grid1.Columns.Count) - 5;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                //moid = -1;
                //lineid = -1;
            }
            
        }

        private void DataGridQuery1_AutoGeneratedColumns(object sender, EventArgs e)
        {
            if (CanUpdate)
            {
                CanUpdate = false;
                btnSave_Click(null, null);
            }
        }

        private void UserVendor_Loaded(object sender, RoutedEventArgs e)
        {
            
            thread = new Thread(UpdateTextRight);
            thread.Start();

        }
        private delegate void outputDelegate(DataTable dt);
        private void updatedatagrid(DataTable dt)
        {
            grid1.ItemsSource = dt.DefaultView;
            double ScreenWith = SystemParameters.WorkArea.Width;// 800;// this.Width;// grid1.Width;
            for (int i = 0; i < grid1.Columns.Count; i++)
            {
                grid1.Columns[i].Width= (ScreenWith / grid1.Columns.Count)-5;
            }
        }
        private void UpdateTextRight()
        {
            while (true)
            {
                if (moid != -1 && lineid != -1&& AutoFresh)
                {
                    
                    //string sql;
                    Parameters parameters;
                    parameters = new Parameters()
                    .Add("BomName", zhanwei, SqlDbType.NVarChar, 1024)
                    .Add("MOId", moid, SqlDbType.Int, 50)
                    .Add("LineId", lineid, SqlDbType.Int, 50)
                    .Add("PartCode", partcode, SqlDbType.NVarChar, 70);
                    DataTable dt = null;
                    Result<Parameters, DataSet> result = null;
                    try
                    {
                        result = DB.DBHelper.ExecuteParametersSource("Get_SMT_Material_Report", parameters, ExecuteType.StoredProcedure);
                        dt = result.Value2.Tables[0];
                        grid1.Dispatcher.Invoke(new outputDelegate(updatedatagrid), dt);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    
                }
                Thread.Sleep(60 * 1000);
            }
        }
        
        public override bool IsCanCloseControl(out string message)
        {
            if(thread.IsAlive)
            {
                thread.Abort();
            }
            return base.IsCanCloseControl(out message);
        }
        private void TbMO_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            string sql;
            Parameters parameters;
            sql = @"SELECT moid from bas_mo where mocode=@MOCode";
            parameters = new Parameters()
            .Add("MOCode", tbMO.Text, SqlDbType.NVarChar, 50);
            DataTable dt = null;
            try
            {
                dt = DB.DBHelper.GetDataTable(sql, parameters, ExecuteType.Text);
                moid = int.Parse(dt.DefaultView[0]["moid"].ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void TbOP_SelectedIndexChanged(object sender, RoutedEventArgs e)
        { 
            //tesetaafsdafsdfafads
            string sql;
            if (tempbom != tbOP.Text)
            {
                Parameters parameters;
                sql = @"SELECT lineid from bas_line where linecode=@LineCode";
                parameters = new Parameters()
                .Add("LineCode", tbOP.Text, SqlDbType.NVarChar, 50);
                tempbom = tbOP.Text;
                DataTable dt = null;
                try
                {
                    dt = DB.DBHelper.GetDataTable(sql, parameters, ExecuteType.Text);
                    lineid = int.Parse(dt.DefaultView[0]["lineid"].ToString());
                    if (moid > 0 && lineid > 0)
                    {
                        AddBomToList();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        private void AddBomToList()
        {
            string sql;
            Parameters parameters;
            sql = @"SELECT DISTINCT(FeedingStationTableCode) FROM Bas_FeedingStationTable WHERE FeedingStationTableId IN(SELECT DISTINCT(BomId) FROM [dbo].[Bas_Bom_Allocation] WHERE [MOId] ='" + moid.ToString()+"' and LineId="+lineid.ToString()+")";
            DataTable dt = null;
            try
            {
                dt = DB.DBHelper.GetDataTable(sql, null, ExecuteType.Text);
                // bomname.ItemsSource = dt.DefaultView[0];
                bomname.Items.Clear();
                foreach (DataRow dr in dt.Rows)
                {
                    bomname.Items.Add(dr["FeedingStationTableCode"].ToString());
                }
                bomname.SelectedIndex = 0;
                //lineid = int.Parse(dt.DefaultView[0]["lineid"].ToString());
                //if (moid > 0 && lineid > 0)
                //{
                //    AddBomToList();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void Auto_Click(object sender, RoutedEventArgs e)
        {

            if (auto.IsChecked == true)
            {
                AutoFresh = true;
            }
            else
            {
                AutoFresh = false;
            }
        }

        private void Barcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            partcode = barcode.Text.Trim();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // PosReader posReader = new PosReader();
            if (dt != null)
            {
                System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
                if (tbMO.Text != string.Empty&& barcode.Text == string.Empty)
                {
                    dialog.FileName = "订单[" + tbMO.Text + "]"+ tbOP .Text+ "上料记录.xls";
                }
                if(barcode.Text!=string.Empty&& tbMO.Text == string.Empty)
                {
                    dialog.FileName = "物料[" + barcode.Text + "]上料记录.xls";
                }
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DataTableToExcel(dt, dialog.FileName.ToString());
                }
            }
        }
        private bool DataTableToExcel(DataTable dt,string FilePath)
        {
            
            DataTable dataTable = new DataTable();
            dataTable = dt;
            DataView dv = dataTable.DefaultView;
            dv.Sort = "扫描日期 desc";
            dataTable = dv.ToTable();
            bool result = false;
            IWorkbook workbook = null;
            FileStream fs = null;
            IRow row = null;
            ISheet sheet = null;
            ICell cell = null;
            try
            {
                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    workbook = new HSSFWorkbook();
                    sheet = workbook.CreateSheet("Sheet1");//创建一个名称为Sheet0的表
                    int rowCount = dataTable.Rows.Count;//行数
                    int columnCount = dataTable.Columns.Count;//列数

                    //设置列头
                    row = sheet.CreateRow(0);//excel第一行设为列头
                    for (int c = 0; c < columnCount; c++)
                    {
                        cell = row.CreateCell(c);
                        cell.SetCellValue(dt.Columns[c].ColumnName);
                    }

                    //设置每行每列的单元格,
                    for (int i = 0; i < rowCount; i++)
                    {
                        row = sheet.CreateRow(i + 1);
                        for (int j = 0; j < columnCount; j++)
                        {
                            cell = row.CreateCell(j);//excel第二行开始写入数据
                            cell.SetCellValue(dataTable.Rows[i][j].ToString());
                        }
                    }
                    using (fs = File.OpenWrite(FilePath))
                    {
                        workbook.Write(fs);//向打开的这个xls文件中写入数据
                        result = true;
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                if (fs != null)
                {
                    fs.Close();
                }
                return false;
            }
        }

        private void Bomname_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(bomname.SelectedItem==null)
            {
                zhanwei = "";
                return;
            }
            zhanwei = bomname.SelectedItem.ToString();
        }

        private void Bomname_DropDownClosed(object sender, EventArgs e)
        {
            //zhanwei = bomname.Text;
        }
    }
 
}
