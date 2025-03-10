using System;
using System.Net;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using QBFC16Lib;
using System.Collections.Generic;
using com.intuit.idn.samples;

namespace com.intuit.idn.samples
{
    public class ItemInventoryQuery
    {
        public void DoItemInventoryQuery()
        {
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager sessionManager = null;

            try
            {
                // Create the session Manager object
                sessionManager = new QBSessionManager();

                // Create the message set request object to hold our request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                BuildItemInventoryQueryRq(requestMsgSet);

                // Connect to QuickBooks and begin a session
                sessionManager.OpenConnection("", "Sample Code from OSR");
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                // Send the request and get the response from QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // End the session and close the connection to QuickBooks
                sessionManager.EndSession();
                sessionBegun = false;
                sessionManager.CloseConnection();
                connectionOpen = false;

                WalkItemInventoryQueryRs(responseMsgSet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                if (sessionBegun)
                {
                    sessionManager.EndSession();
                }
                if (connectionOpen)
                {
                    sessionManager.CloseConnection();
                }
            }
        }

        void BuildItemInventoryQueryRq(IMsgSetRequest requestMsgSet)
        {
            IItemInventoryQuery ItemInventoryQueryRq = requestMsgSet.AppendItemInventoryQueryRq();
            Console.WriteLine("Fetching Item List from QuickBooks...\n");

            // Request necessary fields, including MPN
            ItemInventoryQueryRq.IncludeRetElementList.Add("Name");
            ItemInventoryQueryRq.IncludeRetElementList.Add("SalesPrice");
            ItemInventoryQueryRq.IncludeRetElementList.Add("QuantityOnHand");
            ItemInventoryQueryRq.IncludeRetElementList.Add("ManufacturerPartNumber");  // 🔥 Ensure MPN is requested
        }

        void WalkItemInventoryQueryRs(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return;

            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);

                // Check the response status code
                if (response.StatusCode >= 0 && response.Detail != null)
                {
                    ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtItemInventoryQueryRs)
                    {
                        IItemInventoryRetList ItemInventoryRet = (IItemInventoryRetList)response.Detail;
                        WalkItemInventoryRet(ItemInventoryRet);
                    }
                }
            }
        }

        void WalkItemInventoryRet(IItemInventoryRetList ItemInventoryRetList)
        {
            if (ItemInventoryRetList == null) return;

            Console.WriteLine($"Number of items: {ItemInventoryRetList.Count}");

            for (int i = 0; i < ItemInventoryRetList.Count; i++)
            {
                IItemInventoryRet itemInventoryRet = ItemInventoryRetList.GetAt(i);

                // Extract necessary fields
                string Name = itemInventoryRet.Name.GetValue();
                double? SalesPrice = itemInventoryRet.SalesPrice != null ? (double?)itemInventoryRet.SalesPrice.GetValue() : null;
                string ManufacturerPartNumber = itemInventoryRet.ManufacturerPartNumber != null ? itemInventoryRet.ManufacturerPartNumber.GetValue() : "N/A";  // 🔥 Fetch MPN

                Console.Write($"Item: {i + 1}, Name: {Name}");

                if (SalesPrice.HasValue)
                    Console.Write($", SalesPrice: {SalesPrice.Value}");

                if (!string.IsNullOrEmpty(ManufacturerPartNumber))
                    Console.Write($", MPN: {ManufacturerPartNumber}");

                Console.WriteLine();
            }

            Console.WriteLine("\nItemInventoryQuery completed!!");
        }
    }
}