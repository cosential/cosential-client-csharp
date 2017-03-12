﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cosential.Integrations.Compass.Client.Models;
using RestSharp;

namespace Cosential.Integrations.Compass.Client
{
    [Obsolete("Use PersonnelContext instead", false)]
    public class PersonnelCompassClient : CompassClient
    {
        public PersonnelCompassClient(int firmId, Guid apiKey, string username, string password, Uri host=null) : base(firmId, apiKey, username, password, host)
        {
        }

        #region CRUD

        public IList<Personnel> Create(IEnumerable<Personnel> personnel)
        {
            var request = new RestRequest("personnel", Method.POST) {RequestFormat = DataFormat.Json};
            request.AddBody(personnel);

            var results = Execute<List<Personnel>>(request);
            return results.Data;
        }

        public Personnel Create(Personnel personnel)
        {
            return Create(new[] { personnel }).FirstOrDefault();
        }

        public Personnel Get(int personnelId)
        {
            var request = new RestRequest("personnel/{id}", Method.GET) {RequestFormat = DataFormat.Json};
            request.AddUrlSegment("id", personnelId.ToString());

            var results = Execute<Personnel>(request);
            return results.Data;
        }

        public IList<Personnel> List(int from, int take)
        {
            var request = new RestRequest("personnel", Method.GET) { RequestFormat = DataFormat.Json };
            request.AddQueryParameter("from", from.ToString());
            request.AddQueryParameter("take", take.ToString());

            var results = Execute<List<Personnel>>(request);
            return results.Data;
        }

        public Personnel Update(Personnel personnel)
        {
            var request = new RestRequest("personnel/{id}", Method.PUT) { RequestFormat = DataFormat.Json };
            request.AddUrlSegment("id", personnel.PersonnelId.ToString());
            request.AddBody(personnel);

            var results = Execute<Personnel>(request);
            return results.Data;
        }

        public void Delete(int personnelId)
        {
            var request = new RestRequest("personnel/{id}", Method.DELETE) { RequestFormat = DataFormat.Json };
            request.AddUrlSegment("id", personnelId.ToString());

            var results = Execute<Personnel>(request);
        }

        #endregion

        public IEnumerable<PersonnelImageMetadata> GetPersonnelImageData(Personnel personnel)
        {
            var request = new RestRequest("personnel/{id}/images", Method.GET) {RequestFormat = DataFormat.Json};
            request.AddUrlSegment("id", personnel.PersonnelId.ToString());
            var result = Execute<List<PersonnelImageMetadata>>(request);
            return result.Data;
        }

        public bool HasImage(Personnel personnel)
        {
            return GetPersonnelImageData(personnel).Any();
        }

        public bool UploadImage(Personnel personnel, string photoUrl)
        {
            if (string.IsNullOrWhiteSpace(photoUrl) || HasImage(personnel)) return false;

            var request = new RestRequest("/images/personnel/{id}", Method.POST) { RequestFormat = DataFormat.Json };
            request.AddUrlSegment("id", personnel.PersonnelId.ToString());
            request.AddQueryParameter("defaultImage", "true");
            request.AddQueryParameter("url", photoUrl);
            request.AddHeader("Content-Type", "application/json");
            
            var result = Execute(request);
            return result.ResponseStatus == ResponseStatus.Completed;
        }

        #region SEARCH

        public List<Personnel> Search(string query, int from=0,int take=50)
        {
            var request = new RestRequest("personnel/search");
            request.AddQueryParameter("q", query);
            request.AddQueryParameter("from", from.ToString());
            request.AddQueryParameter("take", take.ToString());

            var results = Execute<List<Personnel>>(request);
            return results.Data;
        }

        #endregion

        #region convenience functions

        public Personnel GetByExternalId(string externalId)
        {
            var items = Search($"ExternalId.raw:{externalId}");
            return items.FirstOrDefault(p => p.ExternalId == externalId);
        }

        public UpsertResult<Personnel> Upsert(Personnel personnel)
        {
            return new UpsertResult<Personnel>
            {
                Action = (personnel.PersonnelId.HasValue && personnel.PersonnelId.Value > 0) ? UpsertAction.Updated : UpsertAction.Created,
                Data = (personnel.PersonnelId.HasValue && personnel.PersonnelId.Value > 0) ? Update(personnel) : Create(personnel)
            };
        }

        public List<Office> AddOfficeToPersonnel(int personnelId, string officeName)
        {
            //Data to post
            var data = new List<Office>();

            //Look up office
            var findOfficeRequest = new RestRequest($"firmorgs/offices", Method.GET) {RequestFormat = DataFormat.Json};
            findOfficeRequest.AddQueryParameter("q", $"OfficeName.raw:{officeName}");
            var findOfficeResults = Execute<List<Office>>(findOfficeRequest);

            if (findOfficeResults.Data.Any())
            {
                data.Add(findOfficeResults.Data.First());
            }
            else
            {
                //Add new office
                var addOfficeRequest = new RestRequest($"firmorgs/offices", Method.POST) { RequestFormat = DataFormat.Json };
                addOfficeRequest.AddBody(new List<Office> {new Office {OfficeName = officeName}});
                var addOfficeResponse = Execute<List<Office>>(addOfficeRequest);
                if (addOfficeResponse.Data.Any()) data.Add(addOfficeResponse.Data.First());
                else throw new Exception($"Could not find or create an office named {officeName} in Cosential");
            }

            //Associate the office to the personnel
            var request = new RestRequest($"personnel/{personnelId}/offices", Method.POST) {RequestFormat = DataFormat.Json};
            request.AddBody(data);
            var results = Execute<List<Office>>(request);
            return results.Data;
        }

        public UpsertResult<Personnel> UpsertByExternalId(Personnel personnel)
        {
            var found = GetByExternalId(personnel.ExternalId);
            if (found != null) personnel.PersonnelId = found.PersonnelId;

            return Upsert(personnel);
        }

        #endregion
    }
}