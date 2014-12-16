using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Moq;

namespace Octono.Crm.Testing
{
    public enum StateCode
    {
        Active = 0,
        Inactive = 1
    }

    /// <summary>
    /// Helper class for standing up all of the dependencies and common code when unit testing 
    /// dynamics crm plugins.
    /// </summary>
    public class PluginTestContext<T> where T : Entity,new() 
    {
        public Mock<IPluginExecutionContext> PluginContext = new Mock<IPluginExecutionContext>();
        public Mock<IOrganizationServiceFactory> OrganizationServiceFactory = new Mock<IOrganizationServiceFactory>();
        public Mock<IOrganizationService> OrganizationService = new Mock<IOrganizationService>();
        public Mock<ITracingService> TracingService = new Mock<ITracingService>();


        public T PreImage { get; set; }
        public T PostImage { get; set; }
        public T Target { get; set; }

        public OptionSetValue State{ get; set; }

        public Mock<IServiceProvider> ServiceProvider = new Mock<IServiceProvider>();

        public PluginTestContext(string messageName = null, StateCode stateCode = StateCode.Active)
        {
            PreImage = new T();
            PostImage = new T();
            Target = new T();
            State = new OptionSetValue((int)stateCode);

            if (messageName == "SetStateDynamicEntity" || messageName == "SetState")
            {
                PluginContext.Setup(x => x.InputParameters).Returns(new ParameterCollection { { "State", State} });            
            }
            else 
            {
                PluginContext.Setup(x => x.InputParameters).Returns(new ParameterCollection { { "Target", Target } });            
            }

            if (messageName != "Create")
            {
                PluginContext.Setup(x => x.PreEntityImages).Returns(new EntityImageCollection() { new KeyValuePair<string, Entity>("PreImage", PreImage) });
            }

            PluginContext.Setup(x => x.PostEntityImages).Returns(new EntityImageCollection() { new KeyValuePair<string, Entity>("PostImage", PostImage) });

            OrganizationServiceFactory.Setup(x => x.CreateOrganizationService(It.IsAny<Guid>())).Returns(OrganizationService.Object);
            ServiceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(PluginContext.Object);
            ServiceProvider.Setup(x => x.GetService(typeof(IOrganizationServiceFactory))).Returns(OrganizationServiceFactory.Object);
            ServiceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(TracingService.Object);
            PluginContext.SetupGet(x => x.MessageName).Returns(messageName);
            PluginContext.SetupGet(x => x.SharedVariables).Returns(_sharedVariables);
        }

        /// <summary>
        /// Represents the statuscode that fired a SetStatus request
        /// </summary>
        public ParameterCollection SetTriggeringStatusReason(int statuscode)
        {
            var input = new ParameterCollection {{"Status", new OptionSetValue(statuscode)}};
            PluginContext.Setup(x => x.InputParameters).Returns(input);
            return input;
        }

        public Mock<TU> AddDependency<TU>()  where TU : class
        {
            var dependency = new Mock<TU>();
            ServiceProvider.Setup(x => x.GetService(typeof (TU))).Returns(dependency.Object);
            return dependency;
        }


        ParameterCollection _sharedVariables = new ParameterCollection();
        public void AddSharedVariable(string key, object value)
        {
            _sharedVariables.Add(key, value);
        }
    }
}