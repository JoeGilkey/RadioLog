using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;


namespace MEFedMVVM.ViewModelLocator
{
    public enum ServiceType
    {
        Runtime, DesignTime, Both
    }

    /// <summary>
    /// Attribute used to Export a Service and make it discoverable for the ViewModelLocator
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ExportService : ExportAttribute
    {
        internal const string IsDesignTimeServiceProperty = "IsDesignTimeService";
        internal const string ServiceContractProperty = "ServiceContract";

        /// <summary>
        /// Gets if the service is a Design time service
        /// </summary>
        public ServiceType IsDesignTimeService { get; private set; }

        /// <summary>
        /// Gets the Service contract type
        /// </summary>
        public Type ServiceContract { get; private set; }

        /// <summary>
        /// Constructor for the attribute
        /// </summary>
        /// <param name="isDesignTimeService">Pass true if this service is a design time service. Pass false if this service is a runtime service</param>
        /// <param name="contractType">The Type of service you want this to be exported to (the interface that this service is implementing</param>
        public ExportService(ServiceType serviceType, Type contractType)
            : base(contractType)
        {
            IsDesignTimeService = serviceType;
            ServiceContract = contractType;
        }
    }
}
