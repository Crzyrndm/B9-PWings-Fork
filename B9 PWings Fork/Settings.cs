namespace WingProcedural
{
    public class WPDebug : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Enable Aero Logging")]
        public bool logCAV = false;

        [GameParameters.CustomParameterUI("Enable Update Logging")]
        public bool logUpdate = false;

        [GameParameters.CustomParameterUI("Enable Geometry Logging")]
        public bool logUpdateGeometry = false;

        [GameParameters.CustomParameterUI("Enable Material Logging")]
        public bool logUpdateMaterials = false;

        [GameParameters.CustomParameterUI("Enable Mesh ref Logging")]
        public bool logMeshReferences = false;

        [GameParameters.CustomParameterUI("Enable Check Mesh Logging")]
        public bool logCheckMeshFilter = false;

        [GameParameters.CustomParameterUI("Enable Property Logging")]
        public bool logPropertyWindow = false;

        [GameParameters.CustomParameterUI("Enable Flight Setup Logging")]
        public bool logFlightSetup = false;

        [GameParameters.CustomParameterUI("Enable Field Setup Logging")]
        public bool logFieldSetup = false;

        [GameParameters.CustomParameterUI("Enable Fuel Logging")]
        public bool logFuel = false;

        [GameParameters.CustomParameterUI("Enable Limits Logging")]
        public bool logLimits = false;

        [GameParameters.CustomParameterUI("Enable Events Logging")]
        public bool logEvents = false;

        public override string Title
        {
            get
            {
                return "B9 Procedural Wings";
            }
        }

        public override string Section
        {
            get
            {
                return "Editor Settings";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 20;
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }
    }
}