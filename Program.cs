using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sabre.Trams.AppServer.Client;

class TramsAPIDemo
{
    static async Task RunAsync()
    {

        const string URL = "http://localhost:8085/";
        const string alias = "Automate_CBB_117";
        const string username = "SYSDBA";
        const string password = "masterkey";

        Console.WriteLine($"Connecting to server {URL} ...");
        Session session = new Session(new System.Uri(URL));

        Console.WriteLine($"Logging in, alias = {alias}, username = {username} ...");
        WebResponse loginResponse = await session.Login(new System.Net.NetworkCredential(username, password, alias), 1, "").ConfigureAwait(false);
        loginResponse.Check();
        Console.WriteLine($"Login Successful, Session ID = {session.SessionID}");

        Console.WriteLine("Running Invoice Search... ");
        InvoiceSearch invoiceSearch = new InvoiceSearch(session);
        await invoiceSearch.Search(
            new JObject
                {
                    { InvoiceSearch.Param_IssueDateFrom, new JValue(new DateTime(2000, 1, 1)) },
                    { InvoiceSearch.Param_IssueDateTo, new JValue(new DateTime(2003, 12, 31)) },
                    { InvoiceSearch.Param_InvoiceType,
                        new JArray(Invoice.InvoiceType_Sale)
                    },
                    { InvoiceSearch.Param_InvoiceGroup, "UCLA" },
                    {
                        BaseSearchDataset.Param_IncludeCols,
                        new JArray(InvoiceSearch.Col_Invoice_InvoiceNo)
                    }
                }
        ).ConfigureAwait(false);
        Console.WriteLine("Invoice Search Dataset Result Count: " + invoiceSearch.ResultTable.Rows.Count);

        Invoice invoice = new Invoice(session);

        foreach (System.Data.DataRow invoiceSearchRow in invoiceSearch.ResultTable.Rows)
        {
            await invoice.Load((long)invoiceSearchRow[InvoiceSearch.Col_Invoice_InvoiceNo]).ConfigureAwait(false);
            foreach (System.Data.DataRow invoiceDataRow in invoice.GetNestedRows(invoice.InvoiceTable))
            {
                Console.WriteLine("InvoiceNo = " + (long)invoiceDataRow[Invoice.Field_Invoice_InvoiceNo]);
                foreach (System.Data.DataRow bookingDataRow in invoice.GetNestedRows(invoice.BookingTable))
                {
                    Console.WriteLine("  BookingNo = " + (long)bookingDataRow[Invoice.Field_Booking_BookingNo]);
                    foreach (System.Data.DataRow segmentDataRow in invoice.GetNestedRows(invoice.SegmentTable))
                    {
                        Console.WriteLine("    SegmentNo = " + (long)segmentDataRow[Invoice.Field_Segment_SegmentNo]);
                    }
                }
            }
        }

        Console.WriteLine("Running Profile Search... ");
        ProfileSearch profileSearch = new ProfileSearch(session);
        await profileSearch.Search(
            new JObject
                {
                    { ProfileSearch.Param_TravelerFirstNameCombo, BaseSearchDataset.StringCompare_Equal},
                    { ProfileSearch.Param_TravelerFirstName, "Dan"},
                    { ProfileSearch.Param_TravelerLastNameCombo, BaseSearchDataset.StringCompare_Equal},
                    { ProfileSearch.Param_TravelerLastName, "Palley"},
                    { ProfileSearch.Param_CommType, Profile.CommType_Email},
                    { ProfileSearch.Param_CommValue, "danpalley@gmail.com"},
                    {
                        BaseSearchDataset.Param_IncludeCols,
                        new JArray(ProfileSearch.Col_ProfileNo)
                    }
                }
        ).ConfigureAwait(false);
        Console.WriteLine("profile Search Dataset Result Count: " + profileSearch.ResultTable.Rows.Count);

        Profile profile = new Profile(session);

        foreach (System.Data.DataRow profileSearchRow in profileSearch.ResultTable.Rows)
        {
            await profile.Load((long)profileSearchRow[ProfileSearch.Col_ProfileNo]).ConfigureAwait(false);
            Console.WriteLine((long)profileSearchRow[ProfileSearch.Col_ProfileNo]);
        }

        Console.WriteLine("Create New Profile... ");

        await profile.Prepare().ConfigureAwait(false);
        profile.ProfileRow[Profile.Field_Profile_ProfileType_LinkCode] = Profile.ProfileType_Leisure;
        profile.ProfileRow[Profile.Field_Profile_FirstName] = "Dan";
        profile.ProfileRow[Profile.Field_Profile_LastName] = "Palley";
        profile.AddRow(profile.ProfileRow);

        profile.PassengerRow[Profile.Field_Passenger_FirstName] = "Dan";
        profile.PassengerRow[Profile.Field_Passenger_LastName] = "Palley";
        profile.PassengerRow[Profile.Field_ProfilePassenger_IsPrimary] = "Y";
        System.Data.DataRow newPassengerRow = profile.AddRow(profile.PassengerRow);

        profile.PassengerCommRow[Profile.Field_Comm_CommType_LinkNo] = Profile.CommType_Phone;
        profile.PassengerCommRow[Profile.Field_Comm_IsPrimary] = "Y";
        profile.PassengerCommRow[Profile.Field_Comm_CommValue] = "+1 (310) 259-1949";
        profile.PassengerCommRow[Profile.Field_Comm_Description] = "mobile";
        profile.PassengerCommRow = profile.AddRow(profile.PassengerCommRow);

        profile.PassengerCommRow[Profile.Field_Comm_CommType_LinkNo] = Profile.CommType_Email;
        profile.PassengerCommRow[Profile.Field_Comm_IsPrimary] = "Y";
        profile.PassengerCommRow[Profile.Field_Comm_CommValue] = "danpalley@gmail.com";
        profile.PassengerCommRow = profile.AddRow(profile.PassengerCommRow);

        profile.PassengerRow = newPassengerRow;
        profile.PassengerRow[Profile.Field_Passenger_FirstName] = "Tyson";
        profile.PassengerRow[Profile.Field_Passenger_LastName] = "Palley";
        newPassengerRow = profile.AddRow(profile.PassengerRow);

        profile.PassengerCommRow[Profile.Field_Comm_CommType_LinkNo] = Profile.CommType_Email;
        profile.PassengerCommRow[Profile.Field_Comm_IsPrimary] = "Y";
        profile.PassengerCommRow[Profile.Field_Comm_CommValue] = "tysonpalley@gmail.com";
        profile.PassengerCommRow = profile.AddRow(profile.PassengerCommRow);

        Console.WriteLine("Saving Profile... ");
        await profile.Save().ConfigureAwait(false);

        Console.WriteLine("Create New Invoice... ");

        await invoice.Prepare().ConfigureAwait(false);

        // set the desired column values for the new invoice datarow
        invoice.InvoiceRow[Invoice.Field_Invoice_InvoiceNumber] = await invoice.GetNextInvoiceNumber(Invoice.InvoiceType_Sale, 0).ConfigureAwait(false);
        invoice.InvoiceRow[Invoice.Field_Invoice_InvoiceType_LinkCode] = Invoice.InvoiceType_Sale;
        invoice.InvoiceRow[Invoice.Field_Invoice_IssueDate] = DateTime.Today;
        invoice.InvoiceRow[Invoice.Field_Invoice_Client_LinkNo] = 4;
        invoice.InvoiceRow[Invoice.Field_Invoice_Branch_LinkNo] = 0;
        invoice.InvoiceRow[Invoice.Field_Invoice_RecordLocator] = "ABCDEF";

        // add the new invoice datarow to the invoice table
        invoice.AddRow(invoice.InvoiceRow);

        // set the desired column values for the new booking datarow
        invoice.BookingRow[Invoice.Field_Booking_SubmitTo_LinkCode] = Invoice.SubmitTo_Supplier;
        invoice.BookingRow[Invoice.Field_Booking_Vendor_LinkNo] = 2;
        invoice.BookingRow[Invoice.Field_Booking_ConfirmNo] = "ABC123";

        // add the new booking datarow to the booking table and return a new row
        invoice.BookingRow = invoice.AddRow(invoice.BookingRow);

        // set the desired column values for the new booking datarow
        invoice.BookingRow[Invoice.Field_Booking_SubmitTo_LinkCode] = Invoice.SubmitTo_Supplier;
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

        resCard.ResCardPassengerRow[ResCard.Field_ResCardPassenger_Passenger_LinkNo] = 2;
        resCard.ResCardPassengerRow[ResCard.Field_ResCardPassenger_PassName] = "Sharp/Fred";
        resCard.ResCardPassengerRow[ResCard.Field_ResCardPassenger_PassLastName] = "Sharp";
        resCard.ResCardPassengerRow[ResCard.Field_ResCardPassenger_PassFirstName] = "Fred";
        resCard.ResCardPassengerRow[ResCard.Field_ResCardPassenger_PassType] = "Adult";
        resCard.ResCardPassengerRow[ResCard.Field_ResCardPassenger_PrimaryPassenger] = "Y";
        resCard.AddRow(resCard.ResCardPassengerRow);

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

        foreach (System.Data.DataRow activitySearchRow in activitySearch.ResultTable.Rows)
        {
            await activity.Load((long)activitySearchRow[ActivitySearch.Col_ActivityNo]).ConfigureAwait(false);
            Console.WriteLine((long)activitySearchRow[ActivitySearch.Col_ActivityNo]);
        }

        await invoice.Load(1).ConfigureAwait(false);
        foreach (System.Data.DataRow invoiceDataRow in invoice.GetNestedRows(invoice.InvoiceTable))
        {
            foreach (System.Data.DataRow bookingDataRow in invoice.GetNestedRows(invoice.BookingTable))
            {
                //if ((string)bookingDataRow[Invoice.Field_Booking_ClientPayStatus_LinkCode] != Invoice.PayStatus_Voided)
                //{
                //    bookingDataRow[Invoice.Field_Booking_CommAmt] = 2500;
                    //foreach (System.Data.DataRow agentBkgDataRow in invoice.GetNestedRows(invoice.AgentBkgTable))
                    //{
                    //    if (!AgentBkgPaid(agentBkgDataRow)
                    //    {
                    //Console.WriteLine(bookingDataRow[Invoice.Field_Booking_CommAmt] + " " + agentBkgDataRow[Invoice.Field_AgentBkg_Rate]+ " " + agentBkgDataRow[Invoice.Field_AgentBkg_Amount]);
                    //agentBkgDataRow[Invoice.Field_AgentBkg_Rate] = 4000;
                    //agentBkgDataRow[Invoice.Field_AgentBkg_Amount] =
                    //      Math.Round((double)(Int64)bookingDataRow[Invoice.Field_Booking_CommAmt] * (Int64)agentBkgDataRow[Invoice.Field_AgentBkg_Rate] / BaseDataset.TwoDigitRateDenom);
                    //Console.WriteLine(agentBkgDataRow[Invoice.Field_AgentBkg_Rate] + " " + agentBkgDataRow[Invoice.Field_AgentBkg_Amount]);
                    //    }
                    //}
                //}
            }
        }
        await invoice.Save().ConfigureAwait(false);

        Console.WriteLine("Logging Out");
        await session.Logout().ConfigureAwait(false);
    }
    static void Main()
    {
        RunAsync().Wait();
    }
}
