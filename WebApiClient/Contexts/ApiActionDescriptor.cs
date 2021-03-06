﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApiClient.Attributes;

namespace WebApiClient.Contexts
{
    /// <summary>
    /// 表示请求Api描述
    /// </summary>
    public class ApiActionDescriptor : ICloneable
    {
        /// <summary>
        /// 获取Api名称
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// 获取Api关联的特性
        /// </summary>
        public IApiActionAttribute[] Attributes { get; internal set; }

        /// <summary>
        /// 获取Api关联的过滤器特性
        /// </summary>
        public IApiActionFilterAttribute[] Filters { get; internal set; }

        /// <summary>
        /// 获取Api的参数描述
        /// </summary>
        public ApiParameterDescriptor[] Parameters { get; internal set; }

        /// <summary>
        /// 获取Api的返回描述
        /// </summary>
        public ApiReturnDescriptor Return { get; internal set; }

        /// <summary>
        /// 执行api
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        public Task Execute(ApiActionContext context)
        {
            return this.ExecuteAsync(context).CastResult(this.Return.DataType);
        }

        /// <summary>
        /// 异步执行api
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        private async Task<object> ExecuteAsync(ApiActionContext context)
        {
            var provider = context.HttpApiClientConfig.HttpClientContextProvider;
            context.HttpClientContext = provider.CreateHttpClientContext(context);

            var apiResult = await this.ExecuteInternalAsync(context);

            provider.DisponseHttpClientContext(context.HttpClientContext);
            return apiResult;
        }

        /// <summary>
        /// 异步执行api
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        private async Task<object> ExecuteInternalAsync(ApiActionContext context)
        {
            var apiAction = context.ApiActionDescriptor;

            foreach (var actionAttribute in apiAction.Attributes)
            {
                await actionAttribute.BeforeRequestAsync(context);
            }

            foreach (var parameter in apiAction.Parameters)
            {
                foreach (var parameterAttribute in parameter.Attributes)
                {
                    await parameterAttribute.BeforeRequestAsync(context, parameter);
                }
            }

            foreach (var filter in apiAction.Filters)
            {
                await filter.OnBeginRequestAsync(context);
            }

            // 执行Http请求，获取回复对象
            var httpClient = context.HttpClientContext.HttpClient;
            context.ResponseMessage = await httpClient.SendAsync(context.RequestMessage);

            foreach (var filter in apiAction.Filters)
            {
                await filter.OnEndRequestAsync(context);
            }

            return await apiAction.Return.Attribute.GetTaskResult(context);
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new ApiActionDescriptor
            {
                Name = this.Name,
                Attributes = this.Attributes,
                Return = this.Return,
                Filters = this.Filters,
                Parameters = this.Parameters.Select(item => (ApiParameterDescriptor)item.Clone()).ToArray()
            };
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
