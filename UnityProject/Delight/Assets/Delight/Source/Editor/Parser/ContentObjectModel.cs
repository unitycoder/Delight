﻿#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Xml.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Delight.Editor;
using UnityEngine;
using System.Xml.Serialization;
using ProtoBuf;
#endregion

namespace Delight.Editor.Parser
{
    /// <summary>
    /// Contains information about all content objects in the project.
    /// </summary>
    [ProtoContract]
    public class ContentObjectModel
    {
        #region Fields

        public static string ContentObjectModelFile = "DelightContent.bin";

        [ProtoMember(1, AsReference = true)]
        public List<ViewObject> ViewObjects;

        [ProtoMember(2, AsReference = true)]
        public List<ModelObject> ModelObjects;

        [ProtoMember(3, AsReference = true)]
        public List<StyleObject> StyleObjects;

        [ProtoMember(4, AsReference = true)]
        public List<AssetBundleObject> AssetBundleObjects;

        [ProtoMember(6)]
        public bool AssetsNeedUpdate;

        [ProtoMember(7, AsReference = true)]
        public List<AssetType> AssetTypes;

        private Dictionary<string, ViewObject> _viewObjects;
        private Dictionary<string, ModelObject> _modelObjects;
        private Dictionary<string, StyleObject> _styleObjects;
        private Dictionary<string, AssetType> _assetTypes;
        private static ContentObjectModel _contentObjectModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ContentObjectModel()
        {
            ViewObjects = new List<ViewObject>();
            ModelObjects = new List<ModelObject>();
            StyleObjects = new List<StyleObject>();
            AssetBundleObjects = new List<AssetBundleObject>();            
            AssetTypes = new List<AssetType>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads specified view object, creates new one if it doesn't exist.
        /// </summary>
        public ViewObject LoadViewObject(string viewName)
        {
            if (_viewObjects == null)
            {
                _viewObjects = new Dictionary<string, ViewObject>();
                foreach (var item in ViewObjects)
                {
                    _viewObjects.Add(item.Name, item);
                }
            }

            ViewObject viewObject;
            if (_viewObjects.TryGetValue(viewName, out viewObject))
            {
                return viewObject;
            }

            // create new view object if it doesn't exist
            viewObject = new ViewObject { Name = viewName, TypeName = viewName };
            ViewObjects.Add(viewObject);
            _viewObjects.Add(viewName, viewObject);
            return viewObject;
        }

        /// <summary>
        /// Loads specified asset bundle object, creates new one if it doesn't exist.
        /// </summary>
        public AssetBundleObject LoadAssetBundleObject(string bundleName, string bundlePath)
        {
            var bundle = AssetBundleObjects.FirstOrDefault(x => x.Path.IEquals(bundlePath) && x.Name.IEquals(bundleName));
            if (bundle != null) return bundle;

            var existingBundle = AssetBundleObjects.FirstOrDefault(x => x.Name.IEquals(bundleName));
            if (existingBundle != null && !bundleName.IEquals("Resources"))
            {
                Debug.LogError(String.Format("[Delight] Duplicate asset bundle \"{0}\" at \"{1}\". Bundle with same name exist at \"{2}\". Please make sure the bundle names are unique.", bundleName, bundlePath, existingBundle.Path));
            }

            bundle = new AssetBundleObject { Name = bundleName, Path = bundlePath };
            AssetBundleObjects.Add(bundle);
            return bundle;
        }

        /// <summary>
        /// Loads specified model object, creates new one if it doesn't exist.
        /// </summary>
        public ModelObject LoadModelObject(string modelName, bool createIfNotExist = true)
        {
            if (_modelObjects == null)
            {
                _modelObjects = new Dictionary<string, ModelObject>();
                foreach (var item in ModelObjects)
                {
                    _modelObjects.Add(item.Name, item);
                }
            }

            ModelObject modelObject;
            if (_modelObjects.TryGetValue(modelName, out modelObject))
            {
                return modelObject;
            }

            if (!createIfNotExist)
                return null;

            // create new model object if it doesn't exist
            modelObject = new ModelObject { Name = modelName, PluralName = modelName.Pluralize() };
            ModelObjects.Add(modelObject);
            _modelObjects.Add(modelName, modelObject);
            return modelObject;
        }

        /// <summary>
        /// Loads specified style object, creates new one if it doesn't exist.
        /// </summary>
        public StyleObject LoadStyleObject(string styleName)
        {
            if (_styleObjects == null)
            {
                _styleObjects = new Dictionary<string, StyleObject>();
                foreach (var item in StyleObjects)
                {
                    _styleObjects.Add(item.Name, item);
                }
            }

            StyleObject styleObject;
            if (_styleObjects.TryGetValue(styleName, out styleObject))
            {
                return styleObject;
            }

            // create new style object if it doesn't exist
            styleObject = new StyleObject { Name = styleName };
            StyleObjects.Add(styleObject);
            _styleObjects.Add(styleName, styleObject);
            return styleObject;
        }

        /// <summary>
        /// Gets singleton instance of content object model.
        /// </summary>
        public static ContentObjectModel GetInstance()
        {
            if (_contentObjectModel != null)
                return _contentObjectModel;

            LoadObjectModel();
            return _contentObjectModel;
        }

        /// <summary>
        /// Loads object model if it's not already loaded.
        /// </summary>
        private static void LoadObjectModel()
        {
            // check if file exist
            var modelFilePath = GetContentObjectModelFilePath();
            if (!File.Exists(modelFilePath))
            {
                _contentObjectModel = new ContentObjectModel();
                return;
            }

            // deserialize file
            using (var file = File.OpenRead(modelFilePath))
            {
                Debug.Log("Deserializing " + modelFilePath);
                try
                {
                    _contentObjectModel = Serializer.Deserialize<ContentObjectModel>(file);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError(String.Format("[Delight] Failed to deserialize content object model file \"{0}\". Creating new content model.", ContentObjectModelFile));
                    _contentObjectModel = new ContentObjectModel();
                    return;
                }
            }
        }

        /// <summary>
        /// Saves object model.
        /// </summary>
        public void SaveObjectModel()
        {
            var modelFilePath = GetContentObjectModelFilePath();
            using (var file = File.Open(modelFilePath, FileMode.Create))
            {
                Debug.Log("Serializing " + modelFilePath);
                Serializer.Serialize(file, _contentObjectModel);
            }
        }

        /// <summary>
        /// Clears parsed XML content from the object model. 
        /// </summary>
        public void ClearXmlParsedContent()
        {
            ViewObjects = new List<ViewObject>();
            StyleObjects = new List<StyleObject>();
            _viewObjects = null;
            _styleObjects = null;
        }

        /// <summary>
        /// Clears parsed schema content from the object model. 
        /// </summary>
        public void ClearSchemaContent()
        {
            ModelObjects = new List<ModelObject>();
            _modelObjects = null;
        }

        /// <summary>
        /// Clears parsed asset content from the object model.
        /// </summary>
        public void ClearAssetContent()
        {
            AssetBundleObjects.Clear();
            ClearAssetTypes(true);
        }

        /// <summary>
        /// Gets object model file path.
        /// </summary>
        private static string GetContentObjectModelFilePath()
        {
            // get application path minus "/Assets" folder
            var path = Application.dataPath.Substring(0, Application.dataPath.Length - 7);
            return String.Format("{0}/{1}", path, ContentObjectModelFile);
        }

        /// <summary>
        /// Loads specified asset type, creates new one if it doesn't exist.
        /// </summary>
        public AssetType LoadAssetType(Type type, bool addedFromAssetFile)
        {
            return LoadAssetType(type.Name, type.FullName, addedFromAssetFile);
        }

        /// <summary>
        /// Loads specified asset type, creates new one if it doesn't exist.
        /// </summary>
        public AssetType LoadAssetType(string name, string fullName, bool addedFromAssetFile)
        {
            if (_assetTypes == null)
            {
                _assetTypes = new Dictionary<string, AssetType>();
                foreach (var item in AssetTypes)
                {
                    _assetTypes.Add(item.FullName, item);
                }
            }

            AssetType assetType;
            if (_assetTypes.TryGetValue(fullName, out assetType))
            {
                if (addedFromAssetFile)
                    assetType.AddedFromAssetFile = true;
                else
                    assetType.AddedFromView = true;

                return assetType;
            }

            AssetsNeedUpdate = true;

            // create new theme object if it doesn't exist
            assetType = new AssetType { Name = name, FullName = fullName, AddedFromAssetFile = addedFromAssetFile, AddedFromView = !addedFromAssetFile };
            AssetTypes.Add(assetType);
            _assetTypes.Add(fullName, assetType);

            return assetType;
        }

        /// <summary>
        /// Clears asset types added from either views or asset files.
        /// </summary>
        public void ClearAssetTypes(bool addedFromAssetFiles)
        {
            // remove asset types no longer references from files or views
            for (int i = AssetTypes.Count - 1; i >= 0; --i)
            {
                var item = AssetTypes[i];
                if ((addedFromAssetFiles && !item.AddedFromView) || 
                    (!addedFromAssetFiles && !item.AddedFromAssetFile))
                {
                    AssetTypes.RemoveAt(i);
                    if (_assetTypes != null)
                    {
                        _assetTypes.Remove(item.FullName);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all style property assignments for the specified view object. 
        /// </summary>
        public static List<PropertyAssignment> GetViewObjectStylePropertyAssignments(string viewName)
        {
            var model = GetInstance();

            // get all default style declarations belonging to specified view
            var styleDeclarations = model.StyleObjects.SelectMany(x => 
                x.StyleDeclarations.Where(y => String.IsNullOrEmpty(y.StyleName) && 
                                               y.ViewName.IEquals(viewName)));

            var propertyAssignments = styleDeclarations.SelectMany(x => x.PropertyAssignments);
            return propertyAssignments.ToList();
        }

        /// <summary>
        /// Gets all style property assignments for the specified view declaration and style. 
        /// </summary>
        public static List<PropertyAssignment> GetStylePropertyAssignments(string viewName, string styleName)
        {
            var model = GetInstance();

            // get all default styles belonging to specified view and styleId
            var styleDeclarations = model.StyleObjects.SelectMany(x =>
                x.StyleDeclarations.Where(y => y.StyleName.IEquals(styleName) &&
                                               y.ViewName.IEquals(viewName)));

            var propertyAssignments = new List<PropertyAssignment>();                        
            foreach (var styleDeclaration in styleDeclarations)
            {
                // add property assignments from the styles this style is based on
                var basedOnDeclaration = styleDeclaration.BasedOn;
                while (basedOnDeclaration != null)
                {
                    propertyAssignments.AddRange(basedOnDeclaration.PropertyAssignments);
                    basedOnDeclaration = basedOnDeclaration.BasedOn;
                }

                // add property assignments from this style
                propertyAssignments.AddRange(styleDeclaration.PropertyAssignments);                
            }

            return propertyAssignments;
        }

        #endregion
    }

    #region View Object

    [ProtoContract]
    public class ViewObject
    {
        #region Fields

        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string TypeName;

        [ProtoMember(3)]
        public string Namespace;

        [ProtoMember(4)]
        public string FilePath;

        [ProtoMember(5, AsReference = true)]
        public ViewObject BasedOn;

        [ProtoMember(6)]
        public bool NeedUpdate;

        [ProtoMember(7)]
        public List<PropertyExpression> PropertyExpressions;

        [ProtoMember(8)]
        public List<ViewDeclaration> ViewDeclarations;

        [ProtoMember(9)]
        public bool HasContentTemplate;

        [ProtoMember(10, AsReference = true)]
        public ViewObject ContentTemplate;

        [ProtoMember(11, AsReference = true)]
        public ViewDeclaration ContentContainer;

        public List<MappedPropertyDeclaration> MappedPropertyDeclarations;
        public bool HasUpdatedItsMappedProperties;

        #endregion

        #region Constructor

        public ViewObject()
        {
            PropertyExpressions = new List<PropertyExpression>();
            ViewDeclarations = new List<ViewDeclaration>();
            MappedPropertyDeclarations = new List<MappedPropertyDeclaration>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets property assignments, including those set by styles. 
        /// </summary>
        public List<PropertyAssignment> GetPropertyAssignmentsWithStyle()
        {
            var propertyAssignments = new List<PropertyAssignment>();
            propertyAssignments.AddRange(PropertyExpressions.OfType<PropertyAssignment>());

            var stylePropertyAssignments = ContentObjectModel.GetViewObjectStylePropertyAssignments(Name);
            propertyAssignments.AddRange(stylePropertyAssignments);

            return propertyAssignments;
        }

        public void Clear()
        {
            Name = null;
            Namespace = null;
            TypeName = null;
            FilePath = null;
            BasedOn = null;
            NeedUpdate = false;
            ContentContainer = null;
            ContentTemplate = null;
            HasContentTemplate = false;
            PropertyExpressions.Clear();
            ViewDeclarations.Clear();
        }

        public List<ViewDeclarationInfo> GetViewDeclarations(bool includeInheritedDeclarations)
        {
            // gets all view declarations in the view
            var viewDeclarations = new List<ViewDeclarationInfo>();
            if (includeInheritedDeclarations && BasedOn != null)
            {
                foreach (var declaration in BasedOn.GetViewDeclarations(true))
                {
                    declaration.IsInherited = true;
                    viewDeclarations.Add(declaration);
                }
            }

            foreach (var viewDeclaration in ViewDeclarations)
            {
                viewDeclarations.AddRange(GetViewDeclarations(viewDeclaration));
            }

            return viewDeclarations;
        }

        private List<ViewDeclarationInfo> GetViewDeclarations(ViewDeclaration viewDeclaration)
        {
            var viewDeclarations = new List<ViewDeclarationInfo>();
            viewDeclarations.Add(new ViewDeclarationInfo { Declaration = viewDeclaration });

            foreach (var childDeclaration in viewDeclaration.ChildDeclarations)
            {
                viewDeclarations.AddRange(GetViewDeclarations(childDeclaration));
            }

            return viewDeclarations;
        }

        #endregion
    }

    public class PropertyDeclarationInfo
    {
        public PropertyDeclaration Declaration;
        public bool IsInherited;
        public bool IsMapped;
        public bool IsAssetReference;
        public AssetType AssetType;
        public string TargetObjectName;
        public string TargetPropertyName;

        public string FullTargetPropertyPath
        {
            get
            {
                return IsMapped ? String.Format("{0}.{1}", TargetObjectName, TargetPropertyName) :
                    Declaration.PropertyName;
            }
        }
    }

    public class ViewDeclarationInfo
    {
        public ViewDeclaration Declaration;
        public bool IsInherited;
    }

    /// <summary>
    /// Base class for property expressions.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(1, typeof(PropertyDeclaration))]
    [ProtoInclude(2, typeof(PropertyAssignment))]
    [ProtoInclude(3, typeof(PropertyBinding))]
    [ProtoInclude(4, typeof(PropertyMapping))]
    [ProtoInclude(5, typeof(PropertyRename))]
    [ProtoInclude(6, typeof(InitializerProperty))]
    [ProtoInclude(7, typeof(AttachedProperty))]
    [ProtoInclude(8, typeof(AttachedPropertyAssignment))]
    public class PropertyExpression
    {
        [ProtoMember(101)]
        public int LineNumber;
    }

    /// <summary>
    /// Stores information about a dependency property declaration.
    /// </summary>
    [ProtoContract]
    public class PropertyDeclaration : PropertyExpression
    {
        [ProtoMember(1)]
        public string PropertyName;

        [ProtoMember(2)]
        public string PropertyTypeName;

        [ProtoMember(4)]
        public string AssemblyQualifiedType;

        [ProtoMember(6)]
        public string PropertyTypeFullName;

        [ProtoMember(8)]
        public PropertyDeclarationType DeclarationType;

        [ProtoMember(9, AsReference = true)]
        public AssetType AssetType;
    }

    /// <summary>
    /// Stores information about a dependency property declaration.
    /// </summary>
    public class MappedPropertyDeclaration
    {
        public bool IsViewReference;
        public bool IsAssetReference;
        public string TargetPropertyName;
        public string TargetPropertyTypeFullName;
        public string TargetObjectName;
        public string TargetObjectType;
        public string PropertyName;
        public AssetType AssetType;
        public string TargetAssemblyQualifiedType;

        public string FullTargetPropertyPath
        {
            get
            {
                return String.Format("{0}.{1}", TargetObjectName, TargetPropertyName);
            }
        }
    }

    /// <summary>
    /// Stores information about a mapped view.
    /// </summary>
    [ProtoContract]
    public class PropertyMapping : PropertyExpression
    {
        [ProtoMember(1)]
        public string TargetObjectName;

        [ProtoMember(2)]
        public string MapPattern;
    }

    /// <summary>
    /// Stores information about a renamed property.
    /// </summary>
    [ProtoContract]
    public class PropertyRename : PropertyExpression
    {
        [ProtoMember(1)]
        public string TargetPropertyName;

        [ProtoMember(2)]
        public string NewPropertyName;
    }


    /// <summary>
    /// Stores information about a initializer property.
    /// </summary>
    [ProtoContract]
    public class InitializerProperty : PropertyExpression
    {
        [ProtoMember(1)]
        public string PropertyName;

        [ProtoMember(2)]
        public List<string> Properties;
    }

    /// <summary>
    /// Stores information about an attached property declaration.
    /// </summary>
    [ProtoContract]
    public class AttachedProperty : PropertyExpression
    {
        [ProtoMember(1)]
        public string PropertyName;

        [ProtoMember(2)]
        public string PropertyTypeName;

        [ProtoMember(4)]
        public string AssemblyQualifiedType;

        [ProtoMember(6)]
        public string PropertyTypeFullName;
    }

    /// <summary>
    /// Stores information about a property assignment.
    /// </summary>
    [ProtoContract]
    public class PropertyAssignment : PropertyExpression
    {
        [ProtoMember(1)]
        public string PropertyName;

        [ProtoMember(2)]
        public string PropertyValue;

        [ProtoMember(3)]
        public string StateName;

        [ProtoMember(4)]
        public bool AttachedNeedUpdate;

        public PropertyDeclarationInfo PropertyDeclarationInfo;
    }

    /// <summary>
    /// Stores information about a attached property assignment.
    /// </summary>
    [ProtoContract]
    public class AttachedPropertyAssignment : PropertyExpression
    {
        [ProtoMember(1)]
        public string PropertyName;

        [ProtoMember(2)]
        public string PropertyValue;

        [ProtoMember(3)]
        public string ParentId;

        [ProtoMember(4)]
        public string PropertyTypeName;

        [ProtoMember(5)]
        public string ParentViewName;
    }

    /// <summary>
    /// Stores information about a property binding.
    /// </summary>
    [ProtoContract]
    public class PropertyBinding : PropertyExpression
    {
        [ProtoMember(1)]
        public string PropertyName;

        [ProtoMember(2)]
        public string PropertyBindingString;

        [ProtoMember(3)]
        public BindingType BindingType;

        [ProtoMember(4)]
        public string TransformMethod;

        [ProtoMember(5)]
        public List<PropertyBindingSource> Sources = new List<PropertyBindingSource>();

        [ProtoMember(6)]
        public string FormatString;

        [ProtoMember(7)]
        public string ItemId;
    }

    /// <summary>
    /// Stores information about a property binding source.
    /// </summary>
    [ProtoContract]
    public class PropertyBindingSource
    {
        [ProtoMember(1)]
        public string BindingPath;

        [ProtoMember(2)]
        public BindingSourceTypes SourceTypes;

        [ProtoMember(3)]
        public string Converter;
    }

    /// <summary>
    /// Stores information about a view declaration.
    /// </summary>
    [ProtoContract]
    public class ViewDeclaration
    {
        #region Fields

        [ProtoMember(1)]
        public string ViewName;

        [ProtoMember(2)]
        public string Id;

        [ProtoMember(3)]
        public List<PropertyAssignment> PropertyAssignments;

        [ProtoMember(4)]
        public List<PropertyBinding> PropertyBindings;

        [ProtoMember(5)]
        public List<ViewDeclaration> ChildDeclarations;

        [ProtoMember(6)]
        public int LineNumber;

        [ProtoMember(7)]
        public string Style;

        [ProtoMember(8)]
        public List<AttachedPropertyAssignment> AttachedPropertyAssignments;

        [ProtoMember(9, AsReference = true)]
        public ViewDeclaration ParentDeclaration;

        #endregion

        #region Constructor

        public ViewDeclaration()
        {
            PropertyAssignments = new List<PropertyAssignment>();
            PropertyBindings = new List<PropertyBinding>();
            ChildDeclarations = new List<ViewDeclaration>();
            AttachedPropertyAssignments = new List<AttachedPropertyAssignment>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets property assignments, including those set by styles. 
        /// </summary>
        public List<PropertyAssignment> GetPropertyAssignmentsWithStyle()
        {
            var propertyAssignments = new List<PropertyAssignment>();
            propertyAssignments.AddRange(PropertyAssignments);

            if (!String.IsNullOrEmpty(Style))
            {
                var stylePropertyAssignments = ContentObjectModel.GetStylePropertyAssignments(ViewName, Style);
                propertyAssignments.AddRange(stylePropertyAssignments);
            }

            return propertyAssignments;
        }

        #endregion
    }

    /// <summary>
    /// Property binding type.
    /// </summary>
    public enum BindingType
    {
        SingleBinding = 0,
        MultiBindingTransform = 1,
        MultiBindingFormatString = 2
    }

    /// <summary>
    /// Property binding source type.
    /// </summary>        
    [Flags]
    public enum BindingSourceTypes
    {
        Default = 0,
        TwoWay = 1,
        Negated = 2 | TwoWay,
        Model = 4
    }

    /// <summary>
    /// Property declaration type.
    /// </summary>
    public enum PropertyDeclarationType
    {
        Default = 0,
        Action = 1,
        Template = 2,
        View = 3,
        UnityComponent = 4,
        Asset = 5
    }

    #endregion

    #region Model Object

    [ProtoContract]
    public class ModelObject
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2, AsReference = true)]
        public List<ModelProperty> Properties;

        [ProtoMember(3)]
        public bool NeedUpdate;

        [ProtoMember(4)]
        public string Namespace;

        [ProtoMember(5)]
        public string SchemaFilePath;

        [ProtoMember(6)]
        public string PluralName;

        [ProtoMember(8, AsReference = true)]
        public List<ModelObjectData> Data;

        public ModelObject()
        {
            Properties = new List<ModelProperty>();
            Data = new List<ModelObjectData>();
        }

        public void Clear()
        {
            Properties.Clear();
            Data.Clear();
            Namespace = string.Empty;
        }
    }

    [ProtoContract]
    public class ModelProperty
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string TypeName;

        [ProtoMember(3)]
        public string TypeFullName;

        [ProtoMember(4)]
        public bool IsModelReference;
    }

    [ProtoContract]
    public class ModelObjectData
    {
        [ProtoMember(1)]
        public string Id;

        [ProtoMember(2, AsReference = true)]
        public List<ModelObjectPropertyData> PropertyData;

        [ProtoMember(3)]
        public List<string> BuildTargets;

        public ModelObjectData()
        {
            PropertyData = new List<ModelObjectPropertyData>();
            BuildTargets = new List<string>();
        }
    }

    [ProtoContract]
    public class ModelObjectPropertyData
    {
        [ProtoMember(1, AsReference = true)]
        public ModelProperty Property;

        [ProtoMember(2)]
        public string PropertyValue;

        [ProtoMember(3)]
        public int Line;
    }

    #endregion

    #region Style Object

    [ProtoContract]
    public class StyleObject
    {
        #region Fields

        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string FilePath;

        [ProtoMember(3)]
        public bool NeedUpdate;

        [ProtoMember(4)]
        public List<StyleDeclaration> StyleDeclarations;

        #endregion

        #region Constructor

        public StyleObject()
        {
            StyleDeclarations = new List<StyleDeclaration>();
        }

        #endregion

        #region Methods

        public void Clear()
        {
            Name = null;
            FilePath = null;
            NeedUpdate = false;
            StyleDeclarations.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Stores information about a style declaration.
    /// </summary>
    [ProtoContract]
    public class StyleDeclaration
    {
        #region Fields

        [ProtoMember(1)]
        public string ViewName;

        [ProtoMember(2)]
        public string StyleName;

        [ProtoMember(3)]
        public List<PropertyAssignment> PropertyAssignments;

        [ProtoMember(5)]
        public int LineNumber;

        [ProtoMember(6, AsReference = true)]
        public StyleDeclaration BasedOn;

        #endregion

        #region Constructor

        public StyleDeclaration()
        {
            PropertyAssignments = new List<PropertyAssignment>();
        }

        #endregion
    }

    #endregion

    #region Asset Bundle Objects

    [ProtoContract]
    public class AssetBundleObject
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public int Version;

        [ProtoMember(3)]
        public StorageMode StorageMode;

        [ProtoMember(4)]
        public string Path;

        [ProtoMember(5)]
        public List<UnityAssetObject> AssetObjects;

        [ProtoMember(6)]
        public bool NeedBuild;

        /// <summary>
        /// Bool indicating if this is a resource bundle. 
        /// </summary>
        public bool IsResource
        {
            get
            {
                return Name.IEquals("Resources");
            }
        }

        /// <summary>
        /// Full path to bundle.
        /// </summary>
        public string FullPath
        {
            get
            {
                return String.Format("{0}/{1}/", Path, Name);
            }
        }

        /// <summary>
        /// Loads asset object, creates new one if it doesn't exist.
        /// </summary>
        public UnityAssetObject LoadUnityAssetObject(string assetName, string path, Type type)
        {
            var asset = AssetObjects.FirstOrDefault(x => x.Name.IEquals(assetName) && x.Type.Name.IEquals(type.Name));
            if (asset == null)
            {
                var assetType = ContentObjectModel.GetInstance().LoadAssetType(type, true);
                asset = new UnityAssetObject { Name = assetName, Type = assetType, Path = path, IsResource = IsResource, AssetBundleName = Name };
                AssetObjects.Add(asset);
            }

            // path within bundle can change
            asset.Path = path;
            string relativePath = asset.Path.Substring(FullPath.Length - 1);
            asset.RelativePath = relativePath.Substring(0, relativePath.LastIndexOf(assetName));
            return asset;
        }

        public void RemoveAssetAtPath(string path)
        {
            AssetObjects.RemoveAll(x => x.Path.IEquals(path));
        }

        public AssetBundleObject()
        {
            AssetObjects = new List<UnityAssetObject>();
        }
    }

    [ProtoContract]
    public class UnityAssetObject
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string Path;

        [ProtoMember(3, AsReference = true)]
        public AssetType Type;

        [ProtoMember(4)]
        public string AssetBundleName;

        [ProtoMember(5)]
        public bool IsResource;

        [ProtoMember(6)]
        public string RelativePath;
    }

    [ProtoContract]
    public class AssetType
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string FullName;

        [ProtoMember(3)]
        public bool AddedFromAssetFile;

        [ProtoMember(4)]
        public bool AddedFromView;

        public string FormattedTypeName
        {
            get
            {
                return Name.EndsWith("Asset") ? Name : Name + "Asset";
            }
        }
    }

    #endregion
}

