//
// N-tier C# and SQL program to analyze CTA Ridership data.
//
// Michal Bochnak, mbochn2
// U. of Illinois, Chicago
// CS341, Fall2017
// Project #08
//


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CTA
{

  public partial class Form1 : Form
  {
    private string BuildConnectionString()
    {
      string version = "MSSQLLocalDB";
      string filename = this.txtDatabaseFilename.Text;
      string connectionInfo = String.Format(@"Data Source=(LocalDB)\{0};AttachDbFilename={1};Integrated Security=True;", version, filename);

      return connectionInfo;
    }

    public Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      try
      {
        string filename = this.txtDatabaseFilename.Text;
        BusinessTier.Business bizTier;
        bizTier = new BusinessTier.Business(filename);
        bizTier.TestConnection();
      }
      catch
      {
        //// ignore any exception that occurs, goal is just to startup//}
      }
    }


    //
    // File>>Exit:
    //
    private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
    {
      this.Close();
    }


    //
    // File>>Load Stations:
    //
    private void toolStripMenuItem2_Click(object sender, EventArgs e)
    {
      ClearStationUI(true /*clear stations*/);

      try
      {
        foreach (BusinessTier.CTAStation s in new BusinessTier.Business(this.txtDatabaseFilename.Text).GetStations())
        {
          this.lstStations.Items.Add(s.Name);
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
        MessageBox.Show(msg);
      }
    }


    //
    // User has clicked on a station for more info:
    //
    private void lstStations_SelectedIndexChanged(object sender, EventArgs e)
    {
      // sometimes this event fires, but nothing is selected...
      if (this.lstStations.SelectedIndex < 0)   // so return now in this case:
        return; 
      
      //
      // clear GUI in case this fails:
      //
      ClearStationUI();

      try
      {
        // get stops at the selected station
        BusinessTier.Business bizTier = new BusinessTier.Business(this.txtDatabaseFilename.Text);
        foreach (BusinessTier.CTAStation station in bizTier.GetStations())
        {
          if (station.Name == this.lstStations.Text)
          {
            foreach (BusinessTier.CTAStop stop in bizTier.GetStops(station.ID))
            {
              this.lstStops.Items.Add(stop.Name);
            }
          }
        }

        // grab station name clicked
        string stationName = lstStations.SelectedItem.ToString().Replace("'", "''");
        this.txtStationID.Text = bizTier.GetStationID(stationName);

        // get total ridership
        long totalRidership = bizTier.GetTotalRidershipFor
          (stationName);
        this.txtTotalRidership.Text = string.Format("{0:#,##0}", totalRidership);

        // get daily ridership
        long dailyRidership = totalRidership / bizTier.GetDaysCount(stationName);
        this.txtAvgDailyRidership.Text = string.Format("{0:#,##0}/day", dailyRidership);

        // get avg ridership
        this.txtPercentRidership.Text = string.Format("{0:0.00}%", 
          ((double)totalRidership / bizTier.GetTotalRidership()) * 100f);

        // weekday
        this.txtWeekdayRidership.Text = string.Format("{0:#,##0}",
          (bizTier.GetRidershipByDayType(stationName, "W")));

        // saturday
        this.txtSaturdayRidership.Text = string.Format("{0:#,##0}",
          (bizTier.GetRidershipByDayType(stationName, "A")));

        // sunday / holiday
        this.txtSundayHolidayRidership.Text = string.Format("{0:#,##0}",
          (bizTier.GetRidershipByDayType(stationName, "U")));
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
        MessageBox.Show(msg);
      }
    }

    private void ClearStationUI(bool clearStatations = false)
    {
      ClearStopUI();

      this.txtTotalRidership.Clear();
      this.txtTotalRidership.Refresh();

      this.txtAvgDailyRidership.Clear();
      this.txtAvgDailyRidership.Refresh();

      this.txtPercentRidership.Clear();
      this.txtPercentRidership.Refresh();

      this.txtStationID.Clear();
      this.txtStationID.Refresh();

      this.txtWeekdayRidership.Clear();
      this.txtWeekdayRidership.Refresh();
      this.txtSaturdayRidership.Clear();
      this.txtSaturdayRidership.Refresh();
      this.txtSundayHolidayRidership.Clear();
      this.txtSundayHolidayRidership.Refresh();

      this.lstStops.Items.Clear();
      this.lstStops.Refresh();

      if (clearStatations)
      {
        this.lstStations.Items.Clear();
        this.lstStations.Refresh();
      }
    }


    //
    // user has clicked on a stop for more info:
    //
    private void lstStops_SelectedIndexChanged(object sender, EventArgs e)
    {
      // sometimes this event fires, but nothing is selected...
      if (this.lstStops.SelectedIndex < 0)   // so return now in this case:
        return; 

      //
      // clear GUI in case this fails:
      //
      ClearStopUI();

      //
      // now display info about this stop:
      //

      try
      {
        string stopName = lstStops.SelectedItem.ToString().Replace("'", "''");

        // get stops at the selected station
        BusinessTier.Business bizTier = new BusinessTier.Business(this.txtDatabaseFilename.Text);
        string stationID = bizTier.GetStationID(lstStations.SelectedItem.ToString().Replace("'", "''")); 
        
        foreach (string l in bizTier.GetLinesAt(stopName, stationID))
        {
          this.lstLines.Items.Add(l);
        }

        // ADA accessible
        bool ada = bizTier.IsADA(stopName, stationID);
        if (ada)
          txtAccessible.Text = "Yes";
        else
          txtAccessible.Text = "No";

        // travel direction
        this.txtDirection.Text = bizTier.GetDirection(stopName, stationID);

        // get location
        BusinessTier.Coordinates c = bizTier.GetCoordinates(stopName, stationID);
        this.txtLocation.Text = (string.Format(@"{0:0.0000}, {1:0.0000}",
          c.Latitude, c.Longitude));

      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
        MessageBox.Show(msg);
      }
    }

    private void ClearStopUI()
    {
      this.txtAccessible.Clear();
      this.txtAccessible.Refresh();

      this.txtDirection.Clear();
      this.txtDirection.Refresh();

      this.txtLocation.Clear();
      this.txtLocation.Refresh();

      this.lstLines.Items.Clear();
      this.lstLines.Refresh();
    }


    //
    // Top-10 stations in terms of ridership:
    //
    private void top10StationsByRidershipToolStripMenuItem_Click(object sender, EventArgs e)
    {
      //
      // clear the UI of any current results:
      //
      ClearStationUI(true /*clear stations*/);

      //
      // now load top-10 stations:
      //

      try
      {
        foreach (BusinessTier.CTAStation s in new BusinessTier.Business(this.txtDatabaseFilename.Text).GetTopStations(10))
        {
          this.lstStations.Items.Add(s.Name);
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
        MessageBox.Show(msg);
      }
    }

    //
    // when user updates text box
    //
    private void txtFind_TextChanged(object sender, EventArgs e)
    {
      ClearStationUI(true);

      try
      {
        string phrase = this.txtFind.Text;

        // get matched stations and add them to the list box
        foreach (BusinessTier.CTAStation s in
         new BusinessTier.Business(this.txtDatabaseFilename.Text).
         GetStationsByPhrase(phrase))
        {
          this.lstStations.Items.Add(s.Name);
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);       
      }
    }

    //
    // user clicked 'switch' button, trigger the ADA state in database
    //
    private void button1_Click(object sender, EventArgs e)
    {
      try
      {
        // get needed info
        BusinessTier.Business bizTier = new BusinessTier.Business(this.txtDatabaseFilename.Text);
        string stationID = bizTier.GetStationID(lstStations.SelectedItem.ToString().Replace("'", "''"));
        string stopName = lstStops.SelectedItem.ToString().Replace("'", "''");

        // get ADA state
        bool isADA = bizTier.IsADA(stopName, stationID);

        // trigger ADA state
        if (isADA)
          bizTier.updateADA(stopName, stationID, false);
        else
          bizTier.updateADA(stopName, stationID, true);

        // Update ADA GUI field
        if (bizTier.IsADA(stopName, stationID))
          this.txtAccessible.Text = "Yes";
        else
          this.txtAccessible.Text = "No";
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
      }
    }
  }//class
}//namespace
