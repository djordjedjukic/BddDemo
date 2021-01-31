﻿using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using Digitalis;
using Digitalis.Features;
using Digitalis.Infrastructure;
using Digitalis.Models;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Raven.Client.Documents.Session;
using Specs.Infrastructure;
using Xunit;

namespace Specs.Features.FetchEntryById
{
    [Trait("Fetch entry", "Happy Path")]
    public class HappyPath : Fixture
    {
        private readonly HttpResponseMessage _response;
        private readonly CreateEntry.Command _newEntry;
        private readonly string _newEntryId;
        private readonly Entry _fetchedEntry;

        public HappyPath(WebApplicationFactory<Startup> factory) : base(factory)
        {
            var creatorClient = this.CreateAuthenticatedClient(new []
                {
                    new Claim(DigitalisClaims.CreateNewEntry, "")
                });

            var readerClient = this.CreateAuthenticatedClient(new []
                {
                    new Claim(DigitalisClaims.FetchEntry, "")
                });

            _newEntry = new CreateEntry.Command(new[] { "tag1", "tag2", "tag3" });

            _response = creatorClient.PostAsync("/entry",
                Serialize(_newEntry)).Result;

            _newEntryId = _response.Content.ReadAsStringAsync().Result;

            _response = readerClient.GetAsync($"/entry?id={_newEntryId}").Result;

            _fetchedEntry = Deserialize<Entry>(_response);

            WaitForIndexing(Store);
            WaitForUserToContinueTheTest(Store);
        }

        [Fact(DisplayName = "1. Status 200 is returned")]
        public void StatusReturned()
        {
            _response.StatusCode.Should().Be(200);
        }

        [Fact(DisplayName = "2. Entry is returned")]
        public void EntryReturned()
        {
            _fetchedEntry.Should().NotBeNull();
        }

        [Fact(DisplayName = "3. Entry has expected Id")]
        public void ExpectedId()
        {
            _fetchedEntry.Id.Should().Be(_newEntryId);
        }

        [Fact(DisplayName = "4. Entry has expected content")]
        public void ExpectedContent()
        {
            _fetchedEntry.Tags.Should().BeEquivalentTo(_newEntry.Tags);
        }
    }
}