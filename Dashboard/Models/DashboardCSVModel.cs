//------------------------------------------ START OF LICENSE -----------------------------------------
//Azure Usage Insights Portal
//
//Copyright(c) Microsoft Corporation
//
//All rights reserved.
//
//MIT License
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//associated documentation files (the ""Software""), to deal in the Software without restriction, 
//including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial 
//portions of the Software.
//
//THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------- END OF LICENSE ------------------------------------------
using System;
using System.Collections.Generic;

using Commons;

namespace Dashboard.Models
{
    public class DashboardCSVModel
    {
        public int startDateMonth { get; set; }
        public int startDateDay { get; set; }
        public int startDateYear { get; set; }
        public int endDateMonth { get; set; }
        public int endDateDay { get; set; }
        public int endDateYear { get; set; }
        public bool dailyReport { get; set; }
        public bool detailedReport { get; set; }
        public List<string> selectedUserSubscriptions { get; set; }
        public Dictionary<string, Subscription> userSubscriptionsList { get; set; }
        public Dictionary<string, ReportRequest> repReqsList { get; set; }
        public DashboardCSVModel()
        {
            Reset();
        }
        public void Reset()
        {
            DateTime currentDate = DateTime.Now.AddMonths(-1);

            startDateMonth = currentDate.Month;
            startDateDay = currentDate.Day;
            startDateYear = currentDate.Year;

            currentDate = DateTime.Now;
            endDateMonth = currentDate.Month;
            endDateDay = currentDate.Day;
            endDateYear = currentDate.Year;

            dailyReport = true;

            detailedReport = false;

            selectedUserSubscriptions = new List<string>();

            userSubscriptionsList = new Dictionary<string, Subscription>();
            repReqsList = new Dictionary<string, ReportRequest>();
        }
    }
}