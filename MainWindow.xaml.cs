using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VRegistration.BLL;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using System.IO;

namespace VRegistration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CardConnector mycard;
        DispatcherTimer timer = new DispatcherTimer();
        //ProgressBar pbprogress = new ProgressBar();
        private int time = 1;
        private double rate = 0.00;
        private bool started = false;
        private bool canAdd = true;
        private bool exchangeAdd = true;
        string file = @"REPORT.txt";

        // Used to hold the default color of textboxes
        private Brush colorHolder;

        /// <summary>
        /// Checkin Tab Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            /* if (txtrfid.Text.Trim().Length == 0)
             {
                 MessageBox.Show("There was a previously scanned RFID");
                 return;
             }*/

            //connect Card Reader
            MessageBox.Show(mycard.verifyCard("5") + mycard.verifyCard("9") + mycard.verifyCard("6"));
            if (mycard.connectCard())
            {
                //read the card UID
                string cardUID = mycard.getcardUID();
                txtrfid.Text = txtrfid.Text + "==="  +  cardUID; //displaying on RFID Field
                mycard.submitText("_12345678901234", "5"); // 5 - is the block we are writing data on the card
                mycard.submitText("_12345678901234", "8"); // 5 - is the block we are writing data on the card
                mycard.submitText("_12345678900000", "6"); // 5 - is the block we are writing data on the card
                if (txtrfid.Text.Trim().Length == 0 || txtirid.Text.Trim().Length == 0)
                {
                    MessageBox.Show("Cannot have blank IRID or RFID");
                    return;
                }
                else {
                   // if (tbcMain.SelectedItem == (TabItem)this.FindName("tabCheckIn"))
                        txtrfid.Text = txtrfid.Text + "----" + mycard.verifyCard("5");
                   // else
                      //  txtScan.Text = txtRegIRID.Text + "----" + mycard.verifyCard("5");
                }

                //MessageBox.Show(BusinessCommands.CheckInWizard());
            }
            
        }

        /// <summary>
        /// Login Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (ContentValidation())
                return;

            string u = txtusername.Text;
            string p = PasswordBox.Password;
            string message =  await Task.Run(() => Execute_Login(this,u,p));

            Loading();
            message = Regex.Replace(message, @"\t|\n|\r", "").Trim();

            // Extremely lazy way of comparing, will be redone
            if (message == "[{\"Data\": \"Success\"}]")
            {
                tabCheckIn.Visibility = tabNewRegistration.Visibility = btnLogout.Visibility = plnav.Visibility = Visibility.Visible;
                
                tbcMain.SelectedItem = tabCounter;
                registrar.Content = txtusername.Text;

                txtusername.Clear();
                PasswordBox.Clear();
                txtusername.Background = PasswordBox.Background = colorHolder;

                tabLogin.Visibility = Visibility.Hidden;
                lblErrorPrompt.Visibility = Visibility.Hidden;
                lblHeadText.Visibility = Visibility.Visible;

                txtirid.Focus();
            }

            else if (message == "[{\"Data\": \"Invalid Login Credentials\"}]")
            {
                lblErrorPrompt.Visibility = Visibility.Visible;
                lblHeadText.Visibility = Visibility.Hidden;
                BrushConverter bc = new BrushConverter();
                txtusername.Background = PasswordBox.Background = (Brush)bc.ConvertFrom("#FFdd4b39");
                PasswordBox.Focus();
            }

            else
                MessageBox.Show(message);
        }
       
        /// <summary>
        /// Check In tab IR ID Textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtirid_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtirid.Text.Count() == 8) { 
                getIRDetails(txtirid.Text);
                GetIRBooking(txtirid.Text);
            }
            else
            {
                txtfullname.Content = "";
                txtemailaddress.Content = "";
            }
        }
        private async void getIRDetails(string ir)
        {
            //verify 8 length
            if (ir.Count() == 8)
            {
                string data = await Task.Run(() => Execute_getIRDetails(this, ir));
                Loading();
                if (data != "[]")
                {
                    JArray irdetailitem = JArray.Parse(data); 
                    txtfullname.Content = irdetailitem[0]["irname"].ToString();
                    txtemailaddress.Content = irdetailitem[0]["email"].ToString();
                    txtfullname_Copy.Content = irdetailitem[0]["irname"].ToString();
                    txtemailaddress_Copy.Content = irdetailitem[0]["email"].ToString();
                    foreach (UIElement element in (tabNewRegistration.Content as Grid).Children)
                    {
                            element.IsEnabled = true;
                    }
                    //txtrfid.Focus();
                }
                else
                {
                    txtemailaddress.Content = "NO DATA FOUND.";
                    txtfullname.Content = "NO DATA FOUND.";
                    txtemailaddress_Copy.Content = "NO DATA FOUND.";
                    txtfullname_Copy.Content = "NO DATA FOUND.";
                    disableRegistrationControls();
                }
            }
            else
            {
                txtemailaddress.Content = "NO DATA FOUND.";
                txtfullname.Content = "NO DATA FOUND.";
                disableRegistrationControls();
            }
        }
        
        /// <summary>
        /// on window loading
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtusername.Focus();
            colorHolder = txtusername.Background;
            tabCheckIn.Visibility = tabNewRegistration.Visibility = btnLogout.Visibility = plnav.Visibility = Visibility.Hidden;
            pbprogress.Visibility = Visibility.Hidden;
            reset();
        }

        public MainWindow()
        {
            InitializeComponent();
            mycard = new CardConnector();

            //IsIndeterminate="True" HorizontalAlignment="Left" Height="31" Minimum="0" Maximum="100" Margin="21,797,0,0" VerticalAlignment="Top" Width="801" Grid.ColumnSpan="5"

            pbprogress.IsIndeterminate = true;
            /*pbprogress.HorizontalAlignment = HorizontalAlignment.Left;
            pbprogress.Height = 31;
            pbprogress.Minimum = 0;
            pbprogress.Maximum = 100;
            pbprogress.Margin = new Thickness(21, 797, 0, 0);
            pbprogress.VerticalAlignment = VerticalAlignment.Top;
            pbprogress.Width = 801;
            pbprogress.SetValue(Grid.ColumnSpanProperty, 5);
            pbprogress.Visibility = Visibility.Visible;*/
        }
        private void reset()
        { 
            txtusername.Clear();
            txtrfid.Clear();
            txtirid.Clear();
            PasswordBox.Clear();
            txtRegIRID.Clear();
            txtPayReference.Clear();
            txtNotes.Clear();
            txtScan.Clear();
            txtfullname_Copy.Content = "";
            txtemailaddress_Copy.Content = "";
            gridnewresult.Visibility = Visibility.Collapsed;
          //  txtnewresult.Content = "";

            gridsearch.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Item focus Tab Checkin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!started && tbcMain.SelectedItem == tabCheckIn)
            {
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (send, evnt) => { ChangeValue(); };
                timer.Start();

                started = true;
            }
            else
            {
                time = 0;
            }
        }

        /// <summary>
        /// Verifies whether the inputs for username and password are valid
        /// </summary>
        private bool ContentValidation()
        {
            if (txtusername.Text.Trim().Length == 0 || PasswordBox.Password.Trim().Length == 0)
            {
                BrushConverter bc = new BrushConverter();
                txtusername.Background = PasswordBox.Background = (Brush)bc.ConvertFrom("#FFdd4b39");
                //txtusername.Background = PasswordBox.Background = Brushes.Red;
                txtusername.Focus();

                return true;
            }

            return false;
        }

        private void ChangeValue()
        {
            int sec = time % 60;
            int min = (time / 60) % 60;
            int hr = (time / 60) / 60;

            lblTime.Content = hr.ToString("D2") + ":" + min.ToString("D2") + ":" + sec.ToString("D2");
            time++;
        }

        private string GetIRBooking(string ir)
        {
            if (ir.Count() == 8)
            {
                //Call GetBooking
                string data = BusinessCommands.getPastBookings(ir);
                data = Regex.Replace(data, @"\t|\n|\r", "");
                // Regex above had to be added since the JSON returned also had 'noise'

                if (data != "[]")
                {
                    JArray irdetailitem = JArray.Parse(data);
                    lbltitle.Content = irdetailitem[0]["title"].ToString(); ;
                    lblBookingID.Content = irdetailitem[0]["BookingID"].ToString();
                }

                return data;
            }
            return "";
        }

        private void Loading()
        {
            //safe call
            Dispatcher.Invoke(() =>
            {
                pbprogress.Visibility = pbprogress.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            });
            Thread.Sleep(300);
            //pbprogress.Visibility = pbprogress.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            logout();
        }

        private void logout()
        { 
            timer.Stop();
            time = 0;
            lblTime.Content = "00:00:00";
            registrar.Content = "***************";

            tabLogin.Visibility = Visibility.Visible;
            tbcMain.SelectedItem = tabLogin;
            plnav.Visibility = btnLogout.Visibility = tabCheckIn.Visibility = tabNewRegistration.Visibility = Visibility.Hidden;
            started = false;
            txtoutercashcurrency.Content = "USD";
            txtoutercashcollected.Content = "0";
            txtoutercccurrency.Content = "USD";
            txtoutercccollected.Content = "0";
            txtTicketIssued.Content = "0";
            txtprevirid.Content = "NO IR ID";
            txtprevticket.Content = "NO Ticket Issued";


            txtusername.Focus();
        }

        private void tabNewRegistration_GotFocus(object sender, RoutedEventArgs e)
        {
            if (canAdd)
            {
                cbxPackage.Items.Clear();
                JArray value = JArray.Parse(BusinessCommands.getFeeList());
                cbxPackage.SelectedValuePath = "Value";
                cbxPackage.DisplayMemberPath = "Key";

                for (int i = 0; i < value.Count(); i++)
                {
                    cbxPackage.Items.Add(new KeyValuePair<string, string>((string)value[i]["FeeName"], (string)value[i]["FeePrice"]));
                }

                canAdd = false;
            }
            //reset the clock
            time = 0;
        }

        private void txtRegIRID_TextChanged(object sender, TextChangedEventArgs e)
        {
            //verify 8 length
            if (txtRegIRID.Text.Count() == 8)
            {
                getIRDetails(txtRegIRID.Text);
                string result = GetIRBooking(txtRegIRID.Text);

                //Loading();
                if (result != "[]")
                {
                    MessageBox.Show("IR has a Booking. Cannot Proceed.");
                }
                //Loading();
            }
        }

        private string GetCurrency()
        {
            // Ternary operators with coalesces are so beautiful
            return rbtAED.IsChecked ?? false ? "AED" :
                rbtUSD.IsChecked ?? false ? "USD" :
                rbtEUR.IsChecked ?? false ? "EUR" :
                rbtMYR.IsChecked ?? false ? "MYR" : null;
        }

        private string GetPayMode()
        {
            return rbtAR.IsChecked ?? false ? "AR" :
                rbtCash.IsChecked ?? false ? "Cash" :
                rbtCC.IsChecked ?? false ? "CC" :
                rbtPayment.IsChecked ?? false ? "Payment Gateway" :
                rbtTT.IsChecked ?? false ? "TT" : null;
        }

        //create a booking and pay
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try { 
            KeyValuePair<string, string> holder = (KeyValuePair<string, string>)cbxPackage.SelectedItem;
            string packageValue = holder.Value;

            if (txtRegIRID.Text.Trim().Length != 8 || packageValue.Trim().Length == 0
                || GetCurrency() == null || GetPayMode() == null)
            {
                    // Would need to add some visual cues on what fields are required
                    MessageBox.Show("Please properly fill in the fields");
                return;
            }
            
            //open and read the card rfiD
            //connect Card Reader
            

                PayBooking(Convert.ToDouble(packageValue));
                Loading();

                //Dictionary<RadioButton, Action> rbNewRegister = new Dictionary<RadioButton, Action>();
                //rbNewRegister.Add()

                if (GetPayMode() == "Cash") {
                    //txtoutercashcollected.Content.ToString().Remove(txtoutercashcollected.Content.ToString().Length - 3);
                    txtoutercashcollected.Content = (Convert.ToDouble(txtoutercashcollected.Content) + Convert.ToDouble(packageValue)); //+"\t" + GetCurrency()
                    txtoutercashcurrency.Content = GetCurrency();
                }
                else if(GetPayMode() == "CC")
                {
                    txtoutercccollected.Content = (Convert.ToDouble(txtoutercccollected.Content) + Convert.ToDouble(packageValue)); //+"\t" + GetCurrency()
                    txtoutercccurrency.Content = GetCurrency();
                }
                txtprevirid.Content = txtRegIRID.Text;
                txtTicketIssued.Content = Convert.ToInt16(txtTicketIssued.Content) + 1;
                // txtprevticket.Content = txtScan.Text;

                string txt = txtoutercashcollected.Content + " Cash| " + txtoutercccollected.Content + " CC| " + txtRegIRID.Text + " | "
                   + packageValue.ToString() + " | " + GetCurrency() + " | " + GetPayMode();

                File.AppendAllText(file, txt + Environment.NewLine);
               

            }
            catch(Exception)
            {
                MessageBox.Show("Please properly fill in the fields");
            }
        }

        // async function here //async
        private void PayBooking(double val)
        {
            string data = "1";// await Task.Run(() => Execute_PayBooking(this));
            if (data == "1")
            {
                JArray value = JArray.Parse(BusinessCommands.getExchangeList());
                for (int i = 0; i < value.Count; i++)
                {
                    if ((string)value[i]["currency"] == GetCurrency())
                    {
                        rate = (double)value[i]["rate"];
                        
                        //Console.WriteLine(value[i]["rate"]);
                        break;
                    }

                }
                gridnewresult.Visibility = Visibility.Visible;
                lblHold.Content = val;
                lblRecived.Content = GetCurrency();
                lblRecived_Copy1.Content = GetCurrency();
                lblRecived_Copy2.Content = GetCurrency();
                txtCashRequired.Text = (Math.Round((val * rate), 2)).ToString();
                //txtnewresult.Content = "IR ID Registered";
                //reset();

            }
        }

        private void GetChange(double val)
        {
            txtChangeAmt.Text = Math.Round(((Convert.ToDouble(txtCashAmt.Text) - Convert.ToDouble(lblHold.Content)) * val), 2).ToString();
            lblNoFunds.Visibility = Visibility.Hidden;
        }

        internal string Execute_getIRDetails(MainWindow gui, string ir)
        {
            //load the gui 
            gui.Loading();
            return BusinessCommands.getIR(ir);
        }
        internal string Execute_Login(MainWindow gui, string txtusername, string PasswordBox)
        {
            gui.Loading();
            return BusinessCommands.CheckLogin(txtusername, PasswordBox);
        }
        internal string Execute_PayBooking(MainWindow gui)
        {
            gui.Loading();

            // string ir, string currency, string payment, string reference, string behalf, string barcode, string feeid, string eventid, string time, string registrar
            // Unsure on what parameter names refer to
            return BusinessCommands.PayBooking(gui.txtRegIRID.Text,
                                               gui.GetCurrency(),
                                               gui.GetPayMode(),
                                               gui.txtPayReference.Text,
                                               gui.txtNotes.Text,
                                               gui.txtScan.Text,
                                               gui.cbxPackage.SelectedValue.ToString(),
                                               null, //Send Null
                                               gui.lblTime.Content.ToString(),
                                               gui.registrar.Content.ToString());
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Grid_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            //transfer to Fully Paid Tab
            tbcMain.SelectedItem = tabCheckIn;
        }

        private void Grid_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
            //transfer to new registration Tab
            tbcMain.SelectedItem = tabNewRegistration;
            disableRegistrationControls();
        }

        private void disableRegistrationControls()
        {
            foreach (UIElement element in (tabNewRegistration.Content as Grid).Children)
            {
                if (element != (TextBox)this.FindName("txtRegIRID") && !(element is Label))
                    element.IsEnabled = false;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            tbcMain.SelectedItem = tabCounter;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            tbcMain.SelectedItem = tabCheckIn;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            tbcMain.SelectedItem = tabNewRegistration;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            logout();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            tbcMain.SelectedItem = tabReport;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            //reset();
            //if (mycard.connectCard())
            //{
            //    //read the card UID
            //    string cardUID = mycard.getcardUID();
            //    txtScan.Text = cardUID; //displaying on RFID Field

            //    if (txtScan.Text.Trim().Length == 0 || txtRegIRID.Text.Trim().Length == 0)
            //    {
            //        MessageBox.Show("Cannot have blank IRID or RFID");
            //        return;
            //    }

            //    //PayBooking(Convert.ToDouble(packageValue));
            //    //show new booking id
            //    // Where should I display the new booking ID?
            //}

            MessageBox.Show(mycard.verifyCard("5") + mycard.verifyCard("9") + mycard.verifyCard("6"));
            if (mycard.connectCard())
            {
                //read the card UID
                string cardUID = mycard.getcardUID();
                txtScan.Text = txtScan.Text + "===" + cardUID; //displaying on RFID Field
                mycard.submitText("_12345678901234", "5"); // 5 - is the block we are writing data on the card
                mycard.submitText("_12345678901234", "8"); // 5 - is the block we are writing data on the card
                mycard.submitText("_12345678900000", "6"); // 5 - is the block we are writing data on the card
                if (txtScan.Text.Trim().Length == 0 || txtRegIRID.Text.Trim().Length == 0)
                {
                    MessageBox.Show("Cannot have blank IRID or RFID");
                    return;
                }
                else
                {
                    // if (tbcMain.SelectedItem == (TabItem)this.FindName("tabCheckIn"))
                    txtScan.Text = txtScan.Text + "----" + mycard.verifyCard("5");
                    gridnewresult.Visibility = Visibility.Hidden;
                    txtCashRecived.Text = "0.00";
                    txtCashAmt.Text = "0.00";
                    txtChangeAmt.Text = "0.00";
                    // else
                    //  txtScan.Text = txtRegIRID.Text + "----" + mycard.verifyCard("5");
                }

                //MessageBox.Show(BusinessCommands.CheckInWizard());
            }

        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {

            txtsearchirid.Text = "";
            txtsearchdtcm.Text = "";
            Loading();

            //connect Card Reader
            if (mycard.connectCard())
            {
                //read the card UID
                string cardUID = mycard.getcardUID();
                txtsearchscan.Text = cardUID;

                txtsearchirid.Text = mycard.verifyCard("5");
                txtsearchdtcm.Text = mycard.verifyCard("6");
            }

            //verify with bookings
            gridsearch.Visibility = Visibility.Visible;
            btnSearchVerify.Visibility = Visibility.Hidden;
            Loading();
            txtsearchscan.Focus();
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://vregistration.the-v.net/admin.aspx?page=BookingSearch&id=" + txtsearchirid.Text);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            if (mycard.connectCard())
            {
                mycard.submitText(txtsearchirid.Text, "5"); // 5 - is the block we are writing data on the card
                mycard.submitText(txtsearchdtcm.Text, "6"); // 6 - is the block we are writing data on the card
               

                MessageBox.Show("Write Done. Click to Verify");
            }
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            reset();
            btnSearchVerify.Visibility = Visibility.Visible;
            txtsearchscan.Text = "";

            txtsearchirid.Text = "";
            txtsearchdtcm.Text = "";
        }

        private void txtCashAmt_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void txtChangeAmt_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void txtChangeAmt_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void cbxExchange_GotFocus(object sender, RoutedEventArgs e)
        {
            //if (exchangeAdd)
            //{
            //    cbxExchange.Items.Clear();
            //    JArray value = JArray.Parse(BusinessCommands.getExchangeList());
            //    cbxExchange.SelectedValuePath = "Value";
            //    cbxExchange.DisplayMemberPath = "Key";

            //    for (int i = 0; i < value.Count(); i++)
            //    {
            //        cbxExchange.Items.Add(new KeyValuePair<string, string>((string)value[i]["currency"], (string)value[i]["rate"]));
            //    }

            //    exchangeAdd = false;
            //}
            ////reset the clock
            //time = 0;
        }

        private void cbxExchange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            

        }

        private void txtCashRecived_TextChanged(object sender, TextChangedEventArgs e)
        {

         
            //foreach (JObject obj in value.Children<JObject>())
            //{
            //    foreach (JProperty property in obj.Properties())
            //    {
                    
            //        string currency = property.Name;
            //        string rate = (string)property.Value;
            //        //Console.WriteLine("Name: {0}, Value: {1}", currency, rate);
            //    }
           // }

        }

        private void txtCashAmt_TextChanged(object sender, TextChangedEventArgs e)
        {
           
        }

        private void txtCashRecived_LostFocus(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            txtCashAmt.Text = (Math.Round(Convert.ToDouble(txtCashRecived.Text) / rate, 2)).ToString();
            if (Convert.ToDouble(txtCashAmt.Text) < Convert.ToDouble(lblHold.Content))
                lblNoFunds.Visibility = Visibility.Visible;
            else
            {
                lblNoFunds.Visibility = Visibility.Hidden;
                btnReceive.IsEnabled = true;
                Loading();
                GetChange(rate);
            }
        }
    }
}
