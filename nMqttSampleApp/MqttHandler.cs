/* 
 * nMQTT, a .Net MQTT v3 client implementation.
 * http://wiki.github.com/markallanson/nmqtt
 * 
 * Copyright (c) 2009 Mark Allanson (mark@markallanson.net)
 *
 * Licensed under the MIT License. You may not use this file except 
 * in compliance with the License. You may obtain a copy of the License at
 *
 *     http://www.opensource.org/licenses/mit-license.php
*/

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Nmqtt;
using System.Diagnostics;
using System.Threading;

namespace nMqtt.SampleApp
{
	/// <summary>
	/// Singleton for consuming mqtt functionality.
	/// </summary>
	public class MqttHandler : IDisposable
	{
		private static MqttHandler instance = new MqttHandler ();

        /// <summary>
        /// Stores the underlying core disposables for the topic
        /// </summary>
        private readonly IDictionary<string, IDisposable> topicSubscriptions = new Dictionary<string, IDisposable>(); 

        /// <summary>
        /// The instance of the underlying MqttClient that is connected to the server.
        /// </summary>
		private MqttClient client;

        /// <summary>
        /// Synchronization context that the mqtthandler uses to invoke the message arrived events on the same thread that connected.
        /// </summary>
        private SynchronizationContext syncContext;

		private MqttHandler ()
		{
		}

        /// <summary>
        /// The instance of the MqttHandler that manages to the Mqtt connection.
        /// </summary>
		public static MqttHandler Instance 
		{
			get { return instance; }
		}

        /// <summary>
        /// Connects to the specified mqtt server.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        /// <returns>The state of the connection.</returns>
		public ConnectionState Connect (string server, short port)
		{
			client = new MqttClient (server, port, Options.ClientIdentifier);
            syncContext = SynchronizationContext.Current;

			Trace.WriteLine ("Connecting to " + server + ":" + port.ToString ());
			
			return client.Connect();
		}

        /// <summary>
        /// Disconnects from the Mqtt server.
        /// </summary>
		public void Disconnect()
		{
			client.Dispose();
		}

        /// <summary>
        /// Subscribes to the specified topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="qos">The qos.</param>
		public void Subscribe(string topic, byte qos)
		{
            if (client == null) throw new InvalidOperationException("You must connect before you can subscribe to a topic.");

			var sub = client.ListenTo(topic, (MqttQos)qos)
                            .ObserveOn(SynchronizationContext.Current)
                            .Subscribe(msg => ClientMessageArrived(instance, new MqttMessageEventArgs(msg.Topic, msg.Payload)));
            topicSubscriptions.Add(topic, sub);
		}

        /// <summary>
        /// Unsubscribes from the specified topic.
        /// </summary>
        /// <param name="topic">The topic to unsubscribe.</param>
        public void Unsubscribe(string topic) {
            IDisposable sub;
            if (topicSubscriptions.TryGetValue(topic, out sub)) {
                sub.Dispose();
                topicSubscriptions.Remove(topic);
            }
        }
		
		/// <summary>
		/// Event fired when a message arrives from the remote server.
		/// </summary>
		public event EventHandler<MqttMessageEventArgs> ClientMessageArrived;

	    public void Dispose() {
	        foreach (var topicSubscription in topicSubscriptions) {
	            topicSubscription.Value.Dispose();
	        }
	    }
	}
}
