//
// BusinessTier:  business logic, acting as interface between UI and data store.
//

using System;
using System.Collections.Generic;
using System.Data;


namespace BusinessTier
{

  //
  // Business:
  //
  public class Business
  {
    //
    // Fields:
    //
    private string _DBFile;
    private DataAccessTier.Data dataTier;


    ///
    /// <summary>
    /// Constructs a new instance of the business tier.  The format
    /// of the filename should be either |DataDirectory|\filename.mdf,
    /// or a complete Windows pathname.
    /// </summary>
    /// <param name="DatabaseFilename">Name of database file</param>
    /// 
    public Business(string DatabaseFilename)
    {
      _DBFile = DatabaseFilename;

      dataTier = new DataAccessTier.Data(DatabaseFilename);
    }


    ///
    /// <summary>
    ///  Opens and closes a connection to the database, e.g. to
    ///  startup the server and make sure all is well.
    /// </summary>
    /// <returns>true if successful, false if not</returns>
    /// 
    public bool TestConnection()
    {
      return dataTier.OpenCloseConnection();
    }


    ///
    /// <summary>
    /// Returns all the CTA Stations, ordered by name.
    /// </summary>
    /// <returns>Read-only list of CTAStation objects</returns>
    /// 
    public IReadOnlyList<CTAStation> GetStations()
    {
      List<CTAStation> stations = new List<CTAStation>();

      try
      {
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        DataSet ds = dat.ExecuteNonScalarQuery("SELECT * FROM Stations ORDER BY Name ASC;");
        
        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          stations.Add(new CTAStation(Convert.ToInt32(row["StationID"].ToString()), row["Name"].ToString()));
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStations: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return stations;
    }


    ///
    /// <summary>
    /// Returns the CTA Stops associated with a given station,
    /// ordered by name.
    /// </summary>
    /// <returns>Read-only list of CTAStop objects</returns>
    ///
    public IReadOnlyList<CTAStop> GetStops(int stationID)
    {
      List<CTAStop> stops = new List<CTAStop>();

      try
      {
        // execute query and collect data
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        DataSet ds = dat.ExecuteNonScalarQuery(string.Format(@"
          SELECT * 
          FROM Stops
          INNER JOIN Stations ON Stops.StationID = Stations.StationID
          WHERE Stations.StationID = '{0}'
          ORDER BY Stops.Name ASC;
          ", stationID));

        // fill data into list
        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          stops.Add(new CTAStop(Convert.ToInt32(row["StopID"].ToString()),
            row["Name"].ToString(), Convert.ToInt32(row["stationID"].ToString()),
            row["Direction"].ToString(), Convert.ToBoolean(row["ADA"].ToString()), 
            Convert.ToDouble(row["Latitude"].ToString()),
            Convert.ToDouble(row["Longitude"].ToString()) ) );
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStops: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return stops;
    }


    ///
    /// <summary>
    /// Returns the top N CTA Stations by ridership, 
    /// ordered by name.
    /// </summary>
    /// <returns>Read-only list of CTAStation objects</returns>
    /// 
    public IReadOnlyList<CTAStation> GetTopStations(int N)
    {
      if (N < 1)
        throw new ArgumentException("GetTopStations: N must be positive");

      List<CTAStation> stations = new List<CTAStation>();

      try
      {
        // execute query and collect data
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        DataSet ds = dat.ExecuteNonScalarQuery(string.Format(@"
          SELECT Top {0} Stations.StationID, Name, Sum(DailyTotal) As TotalRiders 
          FROM Riderships
          INNER JOIN Stations ON Riderships.StationID = Stations.StationID 
          GROUP BY Stations.StationID, Name
          ORDER BY TotalRiders DESC;
          ", N));
        
        // fill into list
        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          stations.Add(new CTAStation(Convert.ToInt32(row["StationID"].ToString()), row["Name"].ToString()));
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetTopStations: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return stations;
    } 

    //
    // calculate total ridership for all stations in the database ( sum )
    //
    public long GetTotalRidership()
    {
      long totalRidership = 0;

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        object result = dat.ExecuteScalarQuery(string.Format(@"
          SELECT Sum(Convert(bigint,DailyTotal)) As TotalOverall
          FROM Riderships;
          "));

        // convert
        totalRidership = Convert.ToInt64(result);
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetTotalRidership: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return totalRidership;
    }


    //
    // calculate total ridership for the given station
    //
    public long GetTotalRidershipFor(string stationName)
    {
      long totalRidership = 0;

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        object result = dat.ExecuteScalarQuery(string.Format(@"SELECT Sum
        (CASE WHEN Stations.Name = '{0}' THEN CONVERT(bigint, DailyTotal) ELSE 0 END)
            FROM Riderships, Stations
              WHERE Riderships.StationID = Stations.StationID
        ", stationName));
        
        // convert
        totalRidership = Convert.ToInt64(result);
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetTotalRidershipFor: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return totalRidership;
    }

    //
    // calculate total days when staion was used
    //
    public long GetDaysCount(string stationName)
    {
      long daysCount = 0;

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        object result = dat.ExecuteScalarQuery(string.Format(@"SELECT         
        Sum(CASE WHEN Stations.Name = '{0}' THEN 1 ELSE 0 END) 
          FROM Riderships, Stations
            WHERE Riderships.StationID = Stations.StationID
        ", stationName));

        // convert
        daysCount = Convert.ToInt64(result);
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetDaysCount: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return daysCount;
    }

    //
    // calculate riderships for the given station and given day type
    //
    // dayType can be:
    //  W - Weekday
    //  A - Saturday
    //  U - Sunday, Holiday
    public long GetRidershipByDayType(string stationName, string dayType)
    {
      long daysCount = 0;

      try
      {
        // ececute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        object result = dat.ExecuteScalarQuery(string.Format(@"SELECT         
        SUM(CASE WHEN TypeOfDay = '{0}' THEN Riderships.DailyTotal ELSE 0 END)
          FROM Stations, Riderships
            WHERE Stations.StationID = Riderships.StationID AND
        Stations.Name = '{1}'", dayType, stationName));

        // convert
        daysCount = Convert.ToInt64(result);
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetRidershipByDayType: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return daysCount;
    }


    //
    // returns the list of lines at given stop
    //
    public IReadOnlyList<string> GetLinesAt(string stopName, string stationID)
    {
      List<string> lines = new List<string>();

      try
      {
        // execute scalar
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        DataSet ds = dat.ExecuteNonScalarQuery(string.Format(@"SELECT 
        Color FROM Stops, StopDetails, Lines
          WHERE Stops.stopID = StopDetails.StopID AND 
        StopDetails.LineID = Lines.LineID AND
        Stops.Name = '{0}'
        AND Stops.StationID = '{1}'
        ORDER BY Color", stopName, stationID));

        // fill in the list
        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          lines.Add(row["Color"].ToString());
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetLinesAt: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return lines;
    }
  

    //
    // checks if given stop is ADA accessible
    //
    public bool IsADA(string stopName, string stationID)
    {
      bool isADA = false;

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        object result = dat.ExecuteScalarQuery(string.Format(@"SELECT 
        ADA FROM Stops 
         WHERE Stops.Name = '{0}' AND
         Stops.StationID = '{1}'", stopName, stationID));

        // convert
        isADA = Convert.ToBoolean(result);
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.IsADA: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return isADA;
    }


    //
    // returns the direction of the given stop
    //
    public string GetDirection(string stopName, string stationID)
    {
      string direction = "N/A";

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        object result = dat.ExecuteScalarQuery(string.Format(@"SELECT 
        Direction FROM Stops 
         WHERE Stops.Name = '{0}' AND
         Stops.StationID = '{1}' ", stopName, stationID));

        // convert
        direction = result.ToString();
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.IsADA: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return direction;
    }


    //
    // return the coordinates with the location of the given stop
    //
    public Coordinates GetCoordinates (string stopName, string stationID)
    {
      Coordinates location = new Coordinates(0.0, 0.0);

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        DataSet ds = dat.ExecuteNonScalarQuery(string.Format(@"SELECT
        Latitude, Longitude
        FROM Stops
        WHERE Name = '{0}' AND
              StationID = '{1}';
        ", stopName, stationID));

        // create new Coordinates object using retrieved data
        location = new Coordinates(Convert.ToDouble(ds.Tables["TABLE"].Rows[0]["Latitude"]),
          Convert.ToDouble(ds.Tables["TABLE"].Rows[0]["Longitude"]));
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetCoordinates: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return location;
    }


    //
    // returns ID for the given station
    //
    public string GetStationID(string stationName)
    {
      string id = "N/A";

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        object result = dat.ExecuteScalarQuery(string.Format(@"SELECT 
        StationID FROM Stations
          WHERE Name = '{0}'", stationName));

        // convert
        id = result.ToString();
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStationID: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return id;
    }


    //
    // returns the list of the stations of which name contains given phrase
    //
    public IReadOnlyList<CTAStation> GetStationsByPhrase (string phrase)
    {
      List<CTAStation> stations = new List<CTAStation>();

      try
      {
        // execute query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        DataSet ds = dat.ExecuteNonScalarQuery(string.Format(@"SELECT * FROM Stations
         WHERE Name LIKE '%{0}%'
          ORDER BY Name ASC", phrase));
        
        // fill in the list
        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          stations.Add(new CTAStation(Convert.ToInt32(row["StationID"].ToString()), 
            row["Name"].ToString()));
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStationsByPhrase: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return stations;
    }

    
    //
    // update ADA state to the given value
    //
    public void updateADA(string stopName, string stationID, bool status)
    {
      try
      {
        // execute action query
        DataAccessTier.Data dat = new DataAccessTier.Data(_DBFile);
        dat.ExecuteActionQuery(string.Format(@"UPDATE
        Stops
        SET ADA = '{0}'
        WHERE  Name = '{1}' AND
              StationID = '{2}'", status, stopName, stationID));
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.triggerADA: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }
    }

  }//class
}//namespace
