﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Cinch
{
    /// <summary>
    /// The class all validating ViewModel objects must inherit from. 
    /// Allows changes via a BeginEdit()/EndEdit() combination, and 
    /// provides rollbacks for cancels. This class also support validation
    /// rules that can be added using the AddRule()
    /// method, such as
    /// </summary>
    /// <example>
    /// <code>
    ///     //STEP 1:
    ///     //Declare a static field for the rule, so it is shared for all instance of the
    ///     //same type
    ///     private static SimpleRule quantityRule;
    ///     
    ///     //STEP 2:
    ///     //Set the rule up in the static constructor
    ///     static OrderViewModel()
    ///     {
    ///
    ///        quantityRule = new SimpleRule("DataValue", "Quantity can not be &lt; 0",
    ///                  (Object domainObject)=>
    ///                  {
    ///                      DataWrapper<Int32> obj = (DataWrapper<Int32>)domainObject;
    ///                      return obj.DataValue &lt;= 0;
    ///                  });
    ///     }
    ///     
    ///     //STEP 3:
    ///     //Add the rule in the instance constructor for the field you want to validate
    ///     public OrderViewModel()
    ///     {
    ///         Quantity = new DataWrapper<Int32>(this, quantityChangeArgs);
    ///         //declare the rules
    ///         quantity.AddRule(quantityRule);
    ///     }  
    /// </code>
    /// </example>
    public abstract class ValidatingViewModelBase : ViewModelBase, IDataErrorInfo
    {
        #region Data
        private List<Rule> rules = new List<Rule>();
        #endregion

        #region Public Methods/Properties
        /// <summary>
        /// Gets a value indicating whether or not this domain object is valid. 
        /// </summary>
        public virtual bool IsValid
        {
            get
            {
                return String.IsNullOrEmpty(this.Error);
            }
        }

        /// <summary>
        /// Gets an error message indicating what is wrong with this domain object. 
        /// The default is an empty string ("").
        /// </summary>
        public virtual string Error
        {
            get
            {
                string result = this[string.Empty];
                if (result != null && result.Trim().Length == 0)
                {
                    result = null;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the error message for the property with the given name.
        /// </summary>
        /// <param name="propertyName">The name of the property whose error 
        /// message to get.</param>
        /// <returns>The error message for the property. 
        /// The default is an empty string ("").</returns>
        public virtual string this[string propertyName]
        {
            get
            {
                string result = string.Empty;

                propertyName = CleanString(propertyName);

                foreach (Rule r in GetBrokenRules(propertyName))
                {
                    if (propertyName == string.Empty || r.PropertyName == propertyName)
                    {
                        result += r.Description;
                        result += Environment.NewLine;
                    }
                }
                result = result.Trim();
                if (result.Length == 0)
                {
                    result = null;
                }
                return result;
            }
        }

        /// <summary>
        /// Validates all rules on this domain object, returning a list of the broken rules.
        /// </summary>
        /// <returns>A read-only collection of rules that have been broken.</returns>
        public virtual ReadOnlyCollection<Rule> GetBrokenRules()
        {
            return GetBrokenRules(string.Empty);
        }

        /// <summary>
        /// Validates all rules on this domain object for a given property, 
        /// returning a list of the broken rules.
        /// </summary>
        /// <param name="property">The name of the property to check for. 
        /// If null or empty, all rules will be checked.</param>
        /// <returns>A read-only collection of rules that have been broken.</returns>
        public virtual ReadOnlyCollection<Rule> GetBrokenRules(string property)
        {
            property = CleanString(property);

            List<Rule> broken = new List<Rule>();

            foreach (Rule r in this.rules)
            {
                // Ensure we only validate a rule 
                if (r.PropertyName == property || property == string.Empty)
                {
                    bool isRuleBroken = r.ValidateRule(this);
                    Debug.WriteLine(DateTime.Now.ToLongTimeString() +
                        ": Validating the rule: '" + r.ToString() +
                        "' on object '" + this.ToString() + "'. Result = " +
                        ((isRuleBroken == false) ? "Valid" : "Broken"));

                    if (isRuleBroken)
                    {
                        broken.Add(r);
                    }
                }
            }

            return broken.AsReadOnly();
        }


        /// <summary>
        /// Adds a new rule to the list of rules
        /// </summary>
        /// <param name="newRule">The new rule</param>
        public void AddRule(Rule newRule)
        {
            this.rules.Add(newRule);
        }

        /// <summary>
        /// A helper method that raises the PropertyChanged event for a property.
        /// </summary>
        /// <param name="propertyNames">The names of the properties that changed.</param>
        protected virtual void NotifyChanged(params string[] propertyNames)
        {
            foreach (string name in propertyNames)
            {
                NotifyPropertyChanged(name);
            }
        }

        /// <summary>
        /// Cleans a string by ensuring it isn't null and trimming it.
        /// </summary>
        /// <param name="s">The string to clean.</param>
        protected string CleanString(string s)
        {
            return (s ?? string.Empty).Trim();
        }
        #endregion
    }
}
