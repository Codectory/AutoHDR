﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Permissions;
using System.Security.Principal;

using Windows.Management.Deployment;
using Windows.ApplicationModel;
using AutoActions.Applications;
using AutoActions.Core;

namespace AutoActions.UWP
{
    public class UWPAppsManager : IApplicationProvider
    {
        static UWPAppsManager _instance = null;

        public static UWPAppsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UWPAppsManager();
                return _instance;
            }
        }
        PackageManager manager = new PackageManager();

        private const string xboxPassAppFN = "Microsoft.GamingApp_8wekyb3d8bbwe";

        public string ProviderName => "UWPProvider";

        public string LocalizedCaption => ProjectResources.ProjectLocales.UWPProvider;

        public UWPApp GetUWPApp(string packageNameOrFamilyPackageName, string identity = "")
        {
            var package = manager.FindPackageForUser(WindowsIdentity.GetCurrent().User.Value, packageNameOrFamilyPackageName);
            if (package == null)
                return GetUWPAppCompatible(packageNameOrFamilyPackageName, identity);
            return new UWPApp(package);
        }

        private UWPApp GetUWPAppCompatible(string familyPackageName, string identity)
        {
            foreach (var package in manager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value))
            {
                try
                {
                    UWPApp uwpApp = new UWPApp(package);
                    if (uwpApp.FamilyPackageName.Equals(familyPackageName) && (string.IsNullOrEmpty(identity) || uwpApp.Identity.Equals(identity)))
                        return uwpApp;
                }
                catch {}
            }
            return null;
        }


        public List<ApplicationItemBase> GetApplications()
        {

            Globals.Logs.Add($"Retrieving UWP apps...", false);

            List<ApplicationItemBase> uwpApps = new List<ApplicationItemBase>();
            IEnumerable<Package> packages = manager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);
            try
            {
                foreach (var package in packages)
                {
                    string s = package.DisplayName;
                    if (package.IsFramework || package.IsResourcePackage || package.SignatureKind != PackageSignatureKind.Store )
                    {
                        continue;
                    }

                    try
                    {
                        if (package.InstalledLocation == null)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    try
                    {
                        UWPApp uwpApp = new UWPApp(package);
                        if (!string.IsNullOrEmpty(uwpApp.ApplicationID))
                         uwpApps.Add(new UWPApplicationItem(uwpApp));
                    }
                    catch
                    {
                        continue;
                    }
                }
                return uwpApps.OrderBy(u => u.DisplayName).ToList();
            }
            catch (Exception ex)
            {
                Globals.Logs.AddException($"Retrieving UWP apps failed.", ex);
                throw;
            }
        }

        public void StartUWPApp(string FamilyPackage, string applicationID)
        {
            Process process = new Process();
            process.StartInfo.FileName = "explorer.exe";
            process.StartInfo.Arguments = $"shell:AppsFolder\\{FamilyPackage}!{applicationID}";
            process.Start();

        }
    }
}
