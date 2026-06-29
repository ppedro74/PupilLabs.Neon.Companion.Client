#nullable enable

namespace PupilLabs.Neon.Companion.Client.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    public sealed class ApiEnvelope<TResult>
    {
        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("result")]
        public TResult? Result { get; set; }
    }

    public sealed class StatusUpdate
    {
        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("data")]
        public JToken Data { get; set; } = new JObject();
    }

    public sealed class Phone
    {
        [JsonProperty("ip")]
        public string? Ip { get; set; }

        [JsonProperty("port")]
        public double? Port { get; set; }

        [JsonProperty("device_id")]
        public string? DeviceId { get; set; }

        [JsonProperty("device_name")]
        public string? DeviceName { get; set; }

        [JsonProperty("battery_level")]
        public double? BatteryLevel { get; set; }

        [JsonProperty("battery_state")]
        public BatteryState? BatteryState { get; set; }

        [JsonProperty("memory")]
        public double? Memory { get; set; }

        [JsonProperty("memory_state")]
        public ResourceState? MemoryState { get; set; }

        [JsonProperty("time_echo_port")]
        public int? TimeEchoPort { get; set; }
    }

    public sealed class Hardware
    {
        [JsonProperty("version")]
        public string? Version { get; set; }

        [JsonProperty("world_camera_serial")]
        public string? WorldCameraSerial { get; set; }

        [JsonProperty("glasses_serial")]
        public string? GlassesSerial { get; set; }
    }

    public sealed class NetworkDevice
    {
        [JsonProperty("ip")]
        public string? Ip { get; set; }

        [JsonProperty("device_id")]
        public string? DeviceId { get; set; }

        [JsonProperty("device_name")]
        public string? DeviceName { get; set; }

        [JsonProperty("connected")]
        public bool? Connected { get; set; }
    }

    public sealed class RecordingStart
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }
    }

    public sealed class RecordingStop
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("rec_duration_ns")]
        public long? RecordingDurationNanoseconds { get; set; }
    }

    public sealed class Recording
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("rec_duration_ns")]
        public long? RecordingDurationNanoseconds { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("action")]
        public RecordingAction? Action { get; set; }
    }

    public sealed class RecordingCancel
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }
    }

    public sealed class RecordingEvent
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }

        [JsonProperty("recording_id")]
        public Guid? RecordingId { get; set; }
    }

    public sealed class EventPost
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public sealed class Sensor
    {
        [JsonProperty("sensor")]
        public SensorType? SensorType { get; set; }

        [JsonProperty("conn_type")]
        public SensorConnectionType? ConnectionType { get; set; }

        [JsonProperty("protocol")]
        public string? Protocol { get; set; }

        [JsonProperty("ip")]
        public string? Ip { get; set; }

        [JsonProperty("port")]
        public int? Port { get; set; }

        [JsonProperty("params")]
        public string? Params { get; set; }

        [JsonProperty("connected")]
        public bool? Connected { get; set; }
    }

    public sealed class TemplateItem
    {
        [JsonProperty("choices")]
        public List<string>? Choices { get; set; }

        [JsonProperty("help_text")]
        public string? HelpText { get; set; }

        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("input_type")]
        public TemplateInputType? InputType { get; set; }

        [JsonProperty("required")]
        public bool? Required { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("widget_type")]
        public TemplateWidgetType WidgetType { get; set; }
    }

    public sealed class Template
    {
        [JsonProperty("archived_at")]
        public DateTimeOffset? ArchivedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("is_default_template")]
        public bool? IsDefaultTemplate { get; set; }

        [JsonProperty("items")]
        public List<TemplateItem>? Items { get; set; }

        [JsonProperty("label_ids")]
        public List<Guid>? LabelIds { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }

        [JsonProperty("recording_ids")]
        public List<Guid>? RecordingIds { get; set; }

        [JsonProperty("recording_name_format")]
        public List<string>? RecordingNameFormat { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public sealed class TemplateData : Dictionary<string, List<string>>
    {
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BatteryState
    {
        Ok,
        Low,
        Critical
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResourceState
    {
        Ok,
        Low,
        Critical
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RecordingAction
    {
        Start,
        Stop,
        Save,
        Discard,
        Error
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SensorType
    {
        World,
        Gaze
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SensorConnectionType
    {
        Direct,
        Websocket
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TemplateInputType
    {
        Any,
        Integer,
        Float
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TemplateWidgetType
    {
        Text,
        Paragraph,
        RadioList,
        CheckboxList,
        SectionHeader,
        PageBreak
    }
}