using LEGainer.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace LEGainer.Handlers
{
    public abstract class BaseHandler
    {
        protected HttpContext context { get; private set; }

        public BaseHandler(IHttpContextAccessor accessor)
        {
            this.context = accessor.HttpContext;
        }

        /// <summary>
        /// 输出结果
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async virtual Task WriteResultAsync<T>(Int32 statusCode, T result) where T: IResultBase
        {
            this.context.Response.StatusCode = statusCode;
            await this.context.Response.WriteAsync(JsonSerializer.Serialize(result,new JsonSerializerOptions() { Encoder=JavaScriptEncoder.Create(UnicodeRanges.All)}));
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <returns></returns>
        public abstract Task HandleAsync();
    }
}
