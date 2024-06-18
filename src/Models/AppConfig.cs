namespace Microsoft.Azure.SpaceFx.HostServices.Logging;
public static class Models {
    public class APP_CONFIG : Core.APP_CONFIG {
        [Flags]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum PluginPermissions {
            NONE = 0,
            LOG_MESSAGE_RECEIVED = 1 << 0,
            TELEMETRY_LOG_MESSAGE_RECEIVED = 1 << 1,
            TELEMETRY_METRIC_MESSAGE_RECEIVED = 1 << 2,
            PRE_WRITE_TO_LOG_FILE = 1 << 1,
            POST_WRITE_TO_LOG_FILE = 1 << 2,
            ALL = LOG_MESSAGE_RECEIVED | TELEMETRY_LOG_MESSAGE_RECEIVED | TELEMETRY_METRIC_MESSAGE_RECEIVED | PRE_WRITE_TO_LOG_FILE | POST_WRITE_TO_LOG_FILE
        }

        public class PLUG_IN : Core.Models.PLUG_IN {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public PluginPermissions CALCULATED_PLUGIN_PERMISSIONS {
                get {
                    PluginPermissions result;
                    System.Enum.TryParse(PLUGIN_PERMISSIONS, out result);
                    return result;
                }
            }

            public PLUG_IN() {
                PLUGIN_PERMISSIONS = "";
                PROCESSING_ORDER = 100;
            }
        }
        public int LOG_FILE_MAX_SIZE_KB { get; set; }
        public TimeSpan LOG_FILE_MAX_TTL { get; set; }
        public bool WRITE_TELEMETRY_TO_LOG { get; set; }
        public APP_CONFIG() : base() {
            LOG_FILE_MAX_SIZE_KB = int.Parse(Core.GetConfigSetting("logfilemaxsizekb").Result);
            LOG_FILE_MAX_TTL = TimeSpan.Parse(Core.GetConfigSetting("logfilemaxttl").Result);
            WRITE_TELEMETRY_TO_LOG = bool.Parse(Core.GetConfigSetting("writetelemetrytolog").Result);
        }
    }
}
