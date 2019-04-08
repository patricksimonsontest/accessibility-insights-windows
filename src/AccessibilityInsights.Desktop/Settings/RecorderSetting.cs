// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Axe.Windows.Core.Bases;
using Axe.Windows.Core.Types;
using Axe.Windows.Desktop.Types;
using System.Collections.Generic;
using System.Linq;
using UIAutomationClient;

namespace Axe.Windows.Desktop.Settings
{
    /// <summary>
    /// Event Record Configuration class
    /// it is to keep event recorder config. also it is used in WPF UI(Event Config Window)
    /// </summary>
    public class RecorderSetting : ConfigurationBase
    {
        #region public properties
#pragma warning disable CA2227 // Collection properties should be read only
        /// <summary>
        /// List of Events for recorder setting
        /// </summary>
        public List<RecordEntitySetting> Events { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

#pragma warning disable CA2227 // Collection properties should be read only
        /// <summary>
        /// List of Properties for recorder setting
        /// </summary>
        public List<RecordEntitySetting> Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Global setting listen to Focus Change event
        /// </summary>
        public bool IsListeningFocusChangedEvent { get; set; }

        /// <summary>
        /// Should event recorder ignore individual settings and listen to all events/properties
        /// </summary>
        public bool IsListeningAllEvents { get; set; } = false;

        public TreeScope ListenScope { get; set; }
        #endregion

        public RecorderSetting()
        {
        }

        /// <summary>
        /// Get the list of EventConfigs
        /// </summary>
        /// <param name="isRecorded">select the target mode for recording</param>
        /// <returns></returns>
        public List<RecordEntitySetting> GetEventsConfigs(bool isRecorded)
        {
            return (from e in this.Events
                    where e.IsRecorded == isRecorded
                    select e).ToList();
        }

        /// <summary>
        /// Set the checked state based on id and type
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <param name="val"></param>
        public void SetChecked(int id, RecordEntityType type, bool val, string name = null)
        {
            int change = val ? 1 : -1;
            if (type == RecordEntityType.Event)
            {
                if (id == EventType.UIA_AutomationFocusChangedEventId)
                {
                    this.IsListeningFocusChangedEvent = val;
                }
                else
                {
                    this.Events.Where(e => e.Id == id).First().CheckedCount += change;                    
                }
            }
            else
            {
                if (this.Properties.Where(e => e.Id == id).Count() > 0)
                {
                    this.Properties.Where(e => e.Id == id).First().CheckedCount += change;
                }
                else
                {
                    this.Properties.Add(new RecordEntitySetting()
                    {
                        Type = RecordEntityType.Property,
                        Id = id,
                        Name = name,
                        IsCustom = true,
                        IsRecorded = false,
                        CheckedCount = 1
                    });
                }

            }
        }

        #region Static methods to get instance
        public static RecorderSetting LoadConfiguration(string path)
        {
            // Get Recorder configuration from local location. but if it is not available, get it from default location. 
            RecorderSetting config = new RecorderSetting();
            config = RecorderSetting.LoadFromJSON<RecorderSetting>(path);
            if (config == null)
            {
                config = RecorderSetting.GetDefaultRecordingConfig();
                config.SerializeInJSON(path);
            }
            else
            {
                // check whether there is any new events to be added into configuration. 
                var events = EventType.GetInstance();
                var ms = from e in events.GetKeyValuePairList()
                         where IsNotInList(e.Key, config.Events)
                         select e;

                if(ms.Count() != 0)
                {
                    foreach(var m in ms)
                    {
                        config.Events.Add(new RecordEntitySetting()
                        {
                            Id = m.Key,
                            Name = m.Value,
                            IsRecorded = false,
                            Type = RecordEntityType.Event,
                        });
                    }
                    config.SerializeInJSON(path);
                }
                config.IsListeningAllEvents = false;
            }

            return config;
        }

        /// <summary>
        /// check whether key exist in the given list. 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        private static bool IsNotInList(int key, List<RecordEntitySetting> events)
        {
            return (from e in events
                    where e.Id == key
                    select e).Count() == 0;
        }

        /// <summary>
        /// Get the default Event Recording Configuration
        /// </summary>
        /// <returns></returns>
        public static RecorderSetting GetDefaultRecordingConfig()
        {
            RecorderSetting config = new RecorderSetting
            {
                // Set Individual Event
                Events = (from e in EventType.GetInstance().GetKeyValuePairList()
                          select new RecordEntitySetting()
                          {
                              Type = RecordEntityType.Event,
                              Id = e.Key,
                              Name = e.Value,
                              IsRecorded = false
                          }).ToList(),

                // Set properties
                Properties = (from e in PropertyType.GetInstance().GetKeyValuePairList()
                              select new RecordEntitySetting()
                              {
                                  Type = RecordEntityType.Property,
                                  Id = e.Key,
                                  Name = e.Value,
                                  IsRecorded = false
                              }).ToList(),

                // Set Global event
                IsListeningFocusChangedEvent = true,

                // Individual Event Scope
                ListenScope = TreeScope.TreeScope_Subtree
            };

            return config;
        }
        #endregion

    }
}
