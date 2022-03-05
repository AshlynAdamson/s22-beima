﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static BEIMA.Backend.FT.TestObjects;

namespace BEIMA.Backend.FT
{
    public enum HttpVerb
    {
        GET,
        POST,
    }

    public class BeimaException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public BeimaException(string message, HttpStatusCode status) : base(message)
        {
            StatusCode = status;
        }
    }

    /// <summary>
    /// Class that helps to manage an http client to the backend API.
    /// </summary>
    public class BeimaClient : IDisposable
    {
        /// <summary>
        /// The http client.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Gets and sets the base address of the client.
        /// </summary>
        public Uri? BaseAddress
        {
            get
            {
                return _httpClient.BaseAddress;
            }
            set
            {
                _httpClient.BaseAddress = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseUrl">The base url for the backend API.</param>
        public BeimaClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = new TimeSpan(0, 1, 0);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Sends an http request to the backend server and returns the http response.
        /// </summary>
        /// <param name="route">The url route excluding the base address.</param>
        /// <param name="verb">The http verb of the request (i.e. GET/POST).</param>
        /// <param name="body">The object to send with the request.</param>
        /// <returns>The http response message of the request.</returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<HttpResponseMessage> SendRequest(string route, HttpVerb verb, object? body = null, string queryString = "")
        {
            StringContent? jsonString = null;
            HttpResponseMessage response;

            if (body != null)
            {
                if (body is string)
                {
                    jsonString = new StringContent((string)body);
                }
                else
                {
                    var jsonObject = JsonConvert.SerializeObject(body);
                    jsonString = new StringContent(jsonObject, Encoding.UTF8, "application/json");
                }
            }

            if (!string.IsNullOrEmpty(queryString))
            {
                route = $"{route}?{queryString}";
            }

            switch (verb)
            {
                case HttpVerb.GET:
                    response = await _httpClient.GetAsync(route);
                    break;
                case HttpVerb.POST:
                    response = await _httpClient.PostAsync(route, jsonString);
                    break;
                default:
                    throw new HttpRequestException($"Invalid request verb: {verb}.");
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new BeimaException(content, response.StatusCode);
            }

            return response;
        }

        /// <summary>
        /// Sends a device get request to the BEIMA api.
        /// </summary>
        /// <param name="id">The id of the device.</param>
        /// <returns>The device with the given id.</returns>
        public async Task<Device> GetDevice(string id)
        {
            var response = await SendRequest($"api/device/{id}", HttpVerb.GET);
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Device>(content);
        }

        /// <summary>
        /// Sends a device get list request to the BEIMA api.
        /// </summary>
        /// <returns>The device list.</returns>
        public async Task<List<Device>> GetDeviceList()
        {
            var response = await SendRequest("api/device_list", HttpVerb.GET);
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Device>>(content);
        }

        /// <summary>
        /// Sends a device post request to the BEIMA api.
        /// </summary>
        /// <param name="device">The device to add.</param>
        /// <returns>The id of the new device.</returns>
        public async Task<string> AddDevice(Device device)
        {
            var response = await SendRequest("api/device", HttpVerb.POST, device);
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<string>(content);
        }

        /// <summary>
        /// Sends a device delete request to the BEIMA api.
        /// </summary>
        /// <param name="id">The id of the device to delete.</param>
        /// <returns>True if the deletion was successful, otherwise false.</returns>
        public async Task<bool> DeleteDevice(string id)
        {
            var response = await SendRequest($"api/device/{id}/delete", HttpVerb.POST);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Sends a device update request to the BEIMA api.
        /// </summary>
        /// <param name="device">The device to update.</param>
        /// <returns>The id of the new device.</returns>
        public async Task<Device> UpdateDevice(Device device)
        {
            var response = await SendRequest($"api/device/{device.Id}/update", HttpVerb.POST, device);
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Device>(content);
        }

        /// <summary>
        /// Sends a device type get request to the BEIMA api.
        /// </summary>
        /// <param name="id">The id of the device type.</param>
        /// <returns>The device type with the given id.</returns>
        public async Task<DeviceType> GetDeviceType(string id)
        {
            var response = await SendRequest($"api/device_type/{id}", HttpVerb.GET);
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DeviceType>(content);
        }

        /// <summary>
        /// Sends a device type post request to the BEIMA api.
        /// </summary>
        /// <param name="deviceType">The device type to add.</param>
        /// <returns>The id of the new device type.</returns>
        public async Task<string> AddDeviceType(DeviceTypeAdd deviceType)
        {
            var response = await SendRequest("api/device_type", HttpVerb.POST, deviceType);
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<string>(content);
        }

        /// <summary>
        /// Disposes the http client.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
