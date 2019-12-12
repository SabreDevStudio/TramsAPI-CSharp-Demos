using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sabre.Trams.AppServer.Client;
class TramsAPIDemo
{
    static async Task RunAsync()
    {

        const string URL = "http://localhost:8085/";
        const string alias = "Automate_CBB_117_C";
        const string username = "SYSDBA";
        const string password = "masterkey";

        //const string URL = "http://ltxw1359.sgdcelab.sabre.com/tramsappserverbeta/tramsappserverwebisapi.dll/";
        //const string alias = "automate_cbb_117";
        //const string username = "SYSDBA";
        //const string password = "masterkey";

        const string TBO_Access_Key = "";

        Console.WriteLine("Connecting to server...");
        Session session = new Session(new System.Uri(URL));

        Console.WriteLine("Logging in...");
        WebResponse loginResponse = await session.Login(new System.Net.NetworkCredential(username, password, alias), 1, TBO_Access_Key).ConfigureAwait(false);
        loginResponse.Check();
        Console.WriteLine(FormattableString.Invariant($"Login Successful, Username = {username}, Alias = {alias}, Session ID = {session.SessionID}"));

        Console.WriteLine("Running Invoice Search... ");
        InvoiceSearch invoiceSearch = new InvoiceSearch(session);
        await invoiceSearch.Search(
            new JObject
                {
                    { InvoiceSearch.Param_IssueDateFrom, new JValue(new DateTime(2000, 1, 1)) },
                    { InvoiceSearch.Param_IssueDateTo, new JValue(new DateTime(2003, 12, 31)) },
                    { InvoiceSearch.Param_InvoiceType,
                        new JArray(
                              Invoice.Literal_InvoiceType_Sale,
                              Invoice.Literal_InvoiceType_Refund
                          )
                    },
                    {
                        BaseSearchDataset.Param_IncludeCols,
                        new JArray(
                            InvoiceSearch.Col_Invoice_InvoiceNo,
                            InvoiceSearch.Col_Invoice_IssueDate,
                            InvoiceSearch.Col_Invoice_InvoiceNumber,
                            InvoiceSearch.Col_Invoice_Remarks
                        )
                    }
                }
        ).ConfigureAwait(false);
        Console.WriteLine("Invoice Search Dataset Result Count: " + invoiceSearch.ResultTable.Rows.Count);

        Invoice invoice = new Invoice(session);

        for (int i = 0; i < invoiceSearch.ResultTable.Rows.Count; i++)
        {
            //await invoice.Load((long)invoiceSearch.ResultTable.Rows[i][InvoiceSearch.Col_Invoice_InvoiceNo]);
            Console.WriteLine((long)invoiceSearch.ResultTable.Rows[i][InvoiceSearch.Col_Invoice_InvoiceNo]);
        }

        Console.WriteLine("Create New Profile... ");
        Profile profile = new Profile(session);

        await profile.Prepare().ConfigureAwait(false);
        profile.ProfileRow[Profile.Field_Profile_ProfileType_LinkCode] = Profile.Literal_ProfileType_Leisure;
        profile.ProfileRow[Profile.Field_Profile_Name] = "Palley/Dan";
        profile.AddRow(profile.ProfileRow);

        profile.PassengerRow[Profile.Field_Passenger_FirstName] = "Dan";
        profile.PassengerRow[Profile.Field_Passenger_LastName] = "Palley";
        profile.PassengerRow[Profile.Field_ProfilePassenger_IsPrimary] = "Y";
        profile.PassengerRow = profile.AddRow(profile.PassengerRow);

        profile.PassengerRow[Profile.Field_Passenger_FirstName] = "Tyson";
        profile.PassengerRow[Profile.Field_Passenger_LastName] = "Palley";
        profile.AddRow(profile.PassengerRow);

        Console.WriteLine("Saving Profile... ");
        await profile.Save().ConfigureAwait(false);

        Console.WriteLine("Create New Invoice... ");
        //Invoice invoice = new Invoice(session);

        await invoice.Prepare().ConfigureAwait(false);

        // set the desired column values for the new invoice datarow
        invoice.InvoiceRow[Invoice.Field_Invoice_InvoiceNumber] = await invoice.GetNextInvoiceNumber(Invoice.Literal_InvoiceType_Sale, 0).ConfigureAwait(false);
        invoice.InvoiceRow[Invoice.Field_Invoice_InvoiceType_LinkCode] = Invoice.Literal_InvoiceType_Sale;
        invoice.InvoiceRow[Invoice.Field_Invoice_IssueDate] = DateTime.Today;
        invoice.InvoiceRow[Invoice.Field_Invoice_Client_LinkNo] = 4;
        invoice.InvoiceRow[Invoice.Field_Invoice_Branch_LinkNo] = 0;
        invoice.InvoiceRow[Invoice.Field_Invoice_RecordLocator] = "ABCDEF";

        // add the new invoice datarow to the invoice table
        invoice.AddRow(invoice.InvoiceRow);

        // set the desired column values for the new booking datarow
        invoice.BookingRow[Invoice.Field_Booking_SubmitTo_LinkCode] = Invoice.Literal_SubmitTo_Supplier;
        invoice.BookingRow[Invoice.Field_Booking_Vendor_LinkNo] = 2;
        invoice.BookingRow[Invoice.Field_Booking_ConfirmNo] = "ABC123";

        // add the new booking datarow to the booking table and return a new row
        invoice.BookingRow = invoice.AddRow(invoice.BookingRow);

        // set the desired column values for the new booking datarow
        invoice.BookingRow[Invoice.Field_Booking_SubmitTo_LinkCode] = Invoice.Literal_SubmitTo_Supplier;
        invoice.BookingRow[Invoice.Field_Booking_Vendor_LinkNo] = 3;
        invoice.BookingRow[Invoice.Field_Booking_ConfirmNo] = "DEF456";

        // add the new booking datarow to the booking table
        invoice.AddRow(invoice.BookingRow);

        // set the desired column values for the new segment datarow
        invoice.SegmentRow[Invoice.Field_Segment_IndexNo] = 1;

        // add the new segment datarow to the segment table
        invoice.AddRow(invoice.SegmentRow);

        Console.WriteLine("Saving invoice... ");
        await invoice.Save().ConfigureAwait(false);

        Console.WriteLine("Create New ResCard... ");
        ResCard resCard = new ResCard(session);

        await resCard.Prepare().ConfigureAwait(false);

        resCard.ResCardRow[ResCard.Field_ResCard_CreateDate] = DateTime.Today;
        resCard.ResCardRow[ResCard.Field_ResCard_Profile_Linkno] = 4;
        resCard.AddRow(resCard.ResCardRow);

        resCard.ReservationRow[ResCard.Field_Reservation_Vendor_LinkNo] = 66;
        resCard.ReservationRow[ResCard.Field_Reservation_TravelCategory_LinkNo] = 1;
        resCard.AddRow(resCard.ReservationRow);

        Console.WriteLine("Saving ResCard... ");
        await resCard.Save().ConfigureAwait(false);

        Console.WriteLine("Running Activity Search... ");
        ActivitySearch activitySearch = new ActivitySearch(session);
        await activitySearch.Search(
            new JObject
                {
                    {
                        BaseSearchDataset.Param_IncludeCols,
                        new JArray(
                            ActivitySearch.Col_ActivityNo
                        )
                    }
                }
        ).ConfigureAwait(false);
        Console.WriteLine("Activity Search Dataset Result Count: " + activitySearch.ResultTable.Rows.Count);

        Activity activity = new Activity(session);

        for (int i = 0; i < activitySearch.ResultTable.Rows.Count; i++)
        {
            //await activity.Load((long)activitySearch.ResultTable.Rows[i][ActivitySearch.Col_ActivityNo]).ConfigureAwait(false);
            Console.WriteLine((long)activitySearch.ResultTable.Rows[i][ActivitySearch.Col_ActivityNo]);
        }

        Console.WriteLine("Logging Out");
        await session.Logout().ConfigureAwait(false);
    }
    static void Main()
    {
        RunAsync().Wait();
    }
}
