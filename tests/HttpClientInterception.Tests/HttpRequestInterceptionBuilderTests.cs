﻿// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    public static class HttpRequestInterceptionBuilderTests
    {
        [Fact]
        public static async Task Register_For_Builder_With_Defaults_Registers_Interception()
        {
            // Arrange
            var requestUri = "https://google.com/";
            var expected = new[] { "foo", "bar" };

            var builder = new HttpRequestInterceptionBuilder()
                .ForGet()
                .ForUrl(requestUri)
                .WithJsonContent(expected);

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            var actual = await HttpAssert.GetAsync<string[]>(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task Builder_Consumes_Content_By_Reference()
        {
            // Arrange
            var requestUri = "https://google.com/";

            var expected = new CustomObject()
            {
                FavoriteColor = CustomObject.Color.Blue,
                Number = 2,
                Text = "The elephant",
            };

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(requestUri)
                .WithJsonContent(expected);

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            var actual1 = await HttpAssert.GetAsync<CustomObject>(options, requestUri);

            // Assert
            actual1.ShouldNotBeNull();
            actual1.ShouldNotBeSameAs(expected);
            actual1.FavoriteColor.ShouldBe(CustomObject.Color.Blue);
            actual1.Number.ShouldBe(2);
            actual1.Text.ShouldBe("The elephant");

            // Arrange
            expected.FavoriteColor = CustomObject.Color.Red;
            expected.Number = 42;
            expected.Text = "L'éléphant";

            // Act
            var actual2 = await HttpAssert.GetAsync<CustomObject>(options, requestUri);

            // Assert
            actual2.ShouldNotBeNull();
            actual2.ShouldNotBeSameAs(actual1);
            actual2.FavoriteColor.ShouldBe(CustomObject.Color.Red);
            actual2.Number.ShouldBe(42);
            actual2.Text.ShouldBe("L'éléphant");
        }

        [Fact]
        public static async Task Builder_Uses_Default_Json_Serializer()
        {
            // Arrange
            var requestUri = "https://google.com/";
            var expected = new { mode = EventResetMode.ManualReset };

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(requestUri)
                .WithJsonContent(expected);

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri);

            // Assert
            actual.Equals(@"{""mode"":1}");
        }

        [Fact]
        public static async Task Builder_Uses_Specified_Json_Serializer()
        {
            // Arrange
            var requestUri = "https://google.com/";
            var expected = new { mode = EventResetMode.ManualReset };

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());

            var builder = new HttpRequestInterceptionBuilder()
                .ForPost()
                .ForUrl(requestUri)
                .WithJsonContent(expected, settings);

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            string actual = await HttpAssert.PostAsync(options, requestUri, new { });

            // Assert
            actual.Equals(@"{""mode"":""ManualReset""}");
        }

        [Fact]
        public static async Task Builder_For_Plaintext_String_With_Custom_Status_Code_Registers_Interception()
        {
            // Arrange
            var requestUri = "https://google.com/";
            var expected = "Not found";

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(requestUri)
                .WithStatus(404)
                .WithContent(expected);

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri, HttpStatusCode.NotFound);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task Builder_For_Html_With_Custom_Media_Type_Registers_Interception()
        {
            // Arrange
            var requestUri = "https://google.com/";
            var mediaType = "text/html";
            var expected = "<html>foo></html>";

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(requestUri)
                .WithMediaType(mediaType)
                .WithContent(expected);

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri, mediaType: mediaType);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task Builder_If_Content_String_Is_Null_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string expected = null;

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(requestUri)
                .WithContent(expected);

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri);

            // Assert
            actual.ShouldBe(string.Empty);
        }

        [Fact]
        public static async Task Builder_For_Raw_Bytes_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(requestUri)
                .WithContent(() => new byte[] { 46, 78, 69, 84 });

            var options = new HttpClientInterceptorOptions().Register(builder);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri);

            // Assert
            actual.ShouldBe(".NET");
        }

        [Fact]
        public static void TryGetResponse_Returns_Empty_Response_If_Custom_Response_Header()
        {
            // Arrange
            string url = "https://google.com/";

            var headers = new Dictionary<string, string>()
            {
                { "a", "b" },
                { "c", "d" },
            };

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(url)
                .WithHeaders(headers);

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
            response.Headers.GetValues("a").ShouldBe(new[] { "b" });
            response.Headers.GetValues("c").ShouldBe(new[] { "d" });
        }

        [Fact]
        public static void TryGetResponse_Returns_Empty_Response_If_Custom_Response_Headers()
        {
            // Arrange
            string url = "https://google.com/";

            var headers = new Dictionary<string, ICollection<string>>()
            {
                { "a", new[] { "b" } },
                { "c", new[] { "d", "e" } },
            };

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(url)
                .WithHeaders(headers);

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
            response.Headers.GetValues("a").ShouldBe(new[] { "b" });
            response.Headers.GetValues("c").ShouldBe(new[] { "d", "e" });
        }

        [Fact]
        public static void TryGetResponse_Returns_Empty_Response_If_Individual_Custom_Response_Headers()
        {
            // Arrange
            string url = "https://google.com/";

            var builder = new HttpRequestInterceptionBuilder()
                .ForUrl(url)
                .WithHeader("a", "b")
                .WithHeader("c", "d", "e", "f")
                .WithHeader("c", "d", "e");

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
            response.Headers.GetValues("a").ShouldBe(new[] { "b" });
            response.Headers.GetValues("c").ShouldBe(new[] { "d", "e" });
        }

        [Fact]
        public static async Task Register_Uses_Defaults_For_Plain_Builder()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder();

            // Act
            options.Register(builder);

            // Assert
            (await HttpAssert.GetAsync(options, "http://localhost")).ShouldBeEmpty();
        }

        [Fact]
        public static async Task Register_Builds_Uri_From_Components_From_Builder()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("something.com")
                .ForPort(444)
                .ForPath("my-path")
                .ForQuery("q=1");

            // Act
            options.Register(builder);

            // Assert
            (await HttpAssert.GetAsync(options, "https://something.com:444/my-path?q=1")).ShouldBeEmpty();
        }

        [Fact]
        public static async Task Register_Builds_Uri_From_Components_From_Builder_With_Http_And_Default_Port()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHttp()
                .ForHost("something.com")
                .ForPort(80);

            // Act
            options.Register(builder);

            // Assert
            (await HttpAssert.GetAsync(options, "http://something.com")).ShouldBeEmpty();
        }

        [Fact]
        public static async Task Register_Builds_Uri_From_UriBuilder()
        {
            // Arrange
            var uriBuilder = new UriBuilder("https://github.com/justeat");

            var options = new HttpClientInterceptorOptions();

            HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder().ForUri(uriBuilder);

            // Act
            options.Register(builder);

            // Assert
            (await HttpAssert.GetAsync(options, "https://github.com/justeat")).ShouldBeEmpty();
        }

        [Fact]
        public static void WithContent_Validates_Parameters()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>("builder", () => (null as HttpRequestInterceptionBuilder).WithContent(null as string));
        }

        [Fact]
        public static void WithJsonContent_Validates_Parameters()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder();
            object content = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("builder", () => (null as HttpRequestInterceptionBuilder).WithJsonContent(content));
            Assert.Throws<ArgumentNullException>("content", () => builder.WithJsonContent(content));
        }

        [Fact]
        public static void ForUrl_Validates_Parameters()
        {
            // Arrange
            string uriString = "https://google.com/";

            // Act and Assert
            Assert.Throws<ArgumentNullException>("builder", () => (null as HttpRequestInterceptionBuilder).ForUrl(uriString));
        }

        [Fact]
        public static void ForGet_Validates_Parameters()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>("builder", () => (null as HttpRequestInterceptionBuilder).ForGet());
        }

        [Fact]
        public static void ForPost_Validates_Parameters()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>("builder", () => (null as HttpRequestInterceptionBuilder).ForPost());
        }

        [Fact]
        public static void ForHttp_Validates_Parameters()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>("builder", () => (null as HttpRequestInterceptionBuilder).ForHttp());
        }

        [Fact]
        public static void ForHttps_Validates_Parameters()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>("builder", () => (null as HttpRequestInterceptionBuilder).ForHttps());
        }

        [Fact]
        public static void ForMethod_Throws_If_Method_Is_Null()
        {
            // Arrange
            HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder();

            // Act and Assert
            Assert.Throws<ArgumentNullException>("method", () => builder.ForMethod(null));
        }

        [Fact]
        public static void ForUri_Throws_If_UriBuilder_Is_Null()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder();
            UriBuilder uriBuilder = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("uriBuilder", () => builder.ForUri(uriBuilder));
        }

        private sealed class CustomObject
        {
            internal enum Color
            {
                Red = 1,
                Blue = 2,
            }

            public Color FavoriteColor { get; set; }

            public int Number { get; set; }

            public string Text { get; set; }
        }
    }
}
