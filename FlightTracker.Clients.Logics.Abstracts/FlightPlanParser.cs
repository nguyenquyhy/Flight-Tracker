using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FlightTracker.Clients.Logics
{
    public class FlightPlanParser
    {
        public Task<SimBaseDocument> ParseAsync(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            using (var reader = new StreamReader(stream))
            {
                var serializer = new XmlSerializer(typeof(SimBaseDocument));
                var data = serializer.Deserialize(reader) as SimBaseDocument;
                return Task.FromResult(data);
            }
        }
    }

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot("SimBase.Document", Namespace = "", IsNullable = false)]
    public partial class SimBaseDocument
    {
        /// <remarks/>
        public string Descr { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("FlightPlan.FlightPlan")]
        public SimBaseDocumentFlightPlanFlightPlan FlightPlanFlightPlan { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Type { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SimBaseDocumentFlightPlanFlightPlan
    {

        private string titleField;

        private string fPTypeField;

        private string routeTypeField;
        private string departureIDField;

        private string departureLLAField;

        private string destinationIDField;

        private string destinationLLAField;

        private string departurePositionField;

        private string departureNameField;

        private string destinationNameField;

        private SimBaseDocumentFlightPlanFlightPlanAppVersion appVersionField;

        private SimBaseDocumentFlightPlanFlightPlanATCWaypoint[] aTCWaypointField;

        /// <remarks/>
        public string Title
        {
            get
            {
                return this.titleField;
            }
            set
            {
                this.titleField = value;
            }
        }

        /// <remarks/>
        public string FPType
        {
            get
            {
                return this.fPTypeField;
            }
            set
            {
                this.fPTypeField = value;
            }
        }

        /// <remarks/>
        public string RouteType
        {
            get
            {
                return this.routeTypeField;
            }
            set
            {
                this.routeTypeField = value;
            }
        }

        /// <remarks/>
        public double CruisingAlt { get; set; }

        /// <remarks/>
        public string DepartureID
        {
            get
            {
                return this.departureIDField;
            }
            set
            {
                this.departureIDField = value;
            }
        }

        /// <remarks/>
        public string DepartureLLA
        {
            get
            {
                return this.departureLLAField;
            }
            set
            {
                this.departureLLAField = value;
            }
        }

        /// <remarks/>
        public string DestinationID
        {
            get
            {
                return this.destinationIDField;
            }
            set
            {
                this.destinationIDField = value;
            }
        }

        /// <remarks/>
        public string DestinationLLA
        {
            get
            {
                return this.destinationLLAField;
            }
            set
            {
                this.destinationLLAField = value;
            }
        }

        /// <remarks/>
        public string DeparturePosition
        {
            get
            {
                return this.departurePositionField;
            }
            set
            {
                this.departurePositionField = value;
            }
        }

        /// <remarks/>
        public string DepartureName
        {
            get
            {
                return this.departureNameField;
            }
            set
            {
                this.departureNameField = value;
            }
        }

        /// <remarks/>
        public string DestinationName
        {
            get
            {
                return this.destinationNameField;
            }
            set
            {
                this.destinationNameField = value;
            }
        }

        /// <remarks/>
        public SimBaseDocumentFlightPlanFlightPlanAppVersion AppVersion
        {
            get
            {
                return this.appVersionField;
            }
            set
            {
                this.appVersionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ATCWaypoint")]
        public SimBaseDocumentFlightPlanFlightPlanATCWaypoint[] ATCWaypoint
        {
            get
            {
                return this.aTCWaypointField;
            }
            set
            {
                this.aTCWaypointField = value;
            }
        }
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class SimBaseDocumentFlightPlanFlightPlanAppVersion
    {
        public ushort AppVersionMajor { get; set; }
        public ushort AppVersionMinor { get; set; }
        public ushort AppVersionRevision { get; set; }
        public ushort AppVersionBuild { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SimBaseDocumentFlightPlanFlightPlanATCWaypoint
    {

        private string aTCWaypointTypeField;

        private string worldPositionField;

        private string aTCAirwayField;

        private SimBaseDocumentFlightPlanFlightPlanATCWaypointICAO iCAOField;

        private string idField;

        /// <remarks/>
        public string ATCWaypointType
        {
            get
            {
                return this.aTCWaypointTypeField;
            }
            set
            {
                this.aTCWaypointTypeField = value;
            }
        }

        /// <remarks/>
        public string WorldPosition
        {
            get
            {
                return this.worldPositionField;
            }
            set
            {
                this.worldPositionField = value;
            }
        }

        /// <remarks/>
        public string ATCAirway
        {
            get
            {
                return this.aTCAirwayField;
            }
            set
            {
                this.aTCAirwayField = value;
            }
        }

        /// <remarks/>
        public SimBaseDocumentFlightPlanFlightPlanATCWaypointICAO ICAO
        {
            get
            {
                return this.iCAOField;
            }
            set
            {
                this.iCAOField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SimBaseDocumentFlightPlanFlightPlanATCWaypointICAO
    {

        private string iCAORegionField;

        private string iCAOIdentField;

        /// <remarks/>
        public string ICAORegion
        {
            get
            {
                return this.iCAORegionField;
            }
            set
            {
                this.iCAORegionField = value;
            }
        }

        /// <remarks/>
        public string ICAOIdent
        {
            get
            {
                return this.iCAOIdentField;
            }
            set
            {
                this.iCAOIdentField = value;
            }
        }
    }


}
