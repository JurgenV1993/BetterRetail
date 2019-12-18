﻿using Composite.Core.WebClient.Renderings.Data;
using Composite.Data;
using Composite.Data.Hierarchy;
using Composite.Data.Hierarchy.DataAncestorProviders;
using Composite.Data.Validation.Validators;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using System;

namespace Orckestra.Composer.CompositeC1.DataTypes.Facets
{
    [AutoUpdateble]
    [DataScope("public")]
    [RelevantToUserType("Developer")]
    [DataAncestorProvider(typeof(NoAncestorDataAncestorProvider))]
    [ImmutableTypeId("f904ff4b-79be-47b3-b618-704c401c4e4f")]
    [KeyTemplatedXhtmlRenderer(XhtmlRenderingType.Embedable, "<span>{label}</span>")]
    [KeyPropertyName("Id")]
    [Title("Facet")]
    [LabelPropertyName("Title")]
    public interface IFacet : IData
    {
        [ImmutableFieldId("fba44850-eefa-4f8b-8050-10b1d73b9774")]
        [FunctionBasedNewInstanceDefaultFieldValue("<f:function name=\"Composite.Utils.Guid.NewGuid\" xmlns:f=\"http://www.composite.net" +
                                                   "/ns/function/1.0\" />")]
        [StoreFieldType(PhysicalStoreFieldType.Guid)]
        [FieldPosition(0)]
        [RouteSegment(0)]
        Guid Id { get; set; }

        [ImmutableFieldId("de5e38e9-4330-4484-8c62-6a01bcab1ce6")]
        [StoreFieldType(PhysicalStoreFieldType.String, 128)]
        [NotNullValidator]
        [FieldPosition(0)]
        [StringSizeValidator(0, 128)]
        [DefaultFieldStringValue("")]
        [FormRenderingProfile(Label = "Title")]
        string Title { get; set; }

        [ImmutableFieldId("6028a19b-eabe-4823-80cb-f8b4ceed9d9a")]
        [StoreFieldType(PhysicalStoreFieldType.String, 128)]
        [NotNullValidator]
        [FieldPosition(1)]
        [StringSizeValidator(0, 128)]
        [DefaultFieldStringValue("")]
        [FormRenderingProfile(Label = "Field Name")]
        string FieldName { get; set; }

        [ImmutableFieldId("dd9d5e53-351a-4100-937e-ddc1cc7ee49d")]
        [FormRenderingProfile(Label = "Facet Type", WidgetFunctionMarkup = @"<f:widgetfunction xmlns:f=""http://www.composite.net/ns/function/1.0"" name=""Composite.Widgets.Selector""><f:param xmlns:f=""http://www.composite.net/ns/function/1.0"" name=""Options""><f:function xmlns:f=""http://www.composite.net/ns/function/1.0"" name=""Composite.Utils.String.Split""><f:param name=""String"" value=""SingleSelect,MultiSelect,Range"" /></f:function></f:param></f:widgetfunction>")]
        [FunctionBasedNewInstanceDefaultFieldValue("<f:function xmlns:f=\"http://www.composite.net/ns/function/1.0\" name=\"Composite.Co" +
                                                   "nstant.String\"><f:param name=\"Constant\" value=\"SingleSelect\" /></f:function>")]
        [StoreFieldType(PhysicalStoreFieldType.String, 64)]
        [NotNullValidator]
        [FieldPosition(2)]
        [StringSizeValidator(0, 64)]
        [DefaultFieldStringValue("")]
        string FacetType { get; set; }

        [ImmutableFieldId("055848c5-e07d-4b59-9f11-dadae431328f")]
        [FormRenderingProfile(Label = "Sort Weight")]
        [FunctionBasedNewInstanceDefaultFieldValue("<f:function xmlns:f=\"http://www.composite.net/ns/function/1.0\" name=\"Composite.Co" +
                                                   "nstant.Decimal\"><f:param name=\"Constant\" value=\"0\" /></f:function>")]
        [StoreFieldType(PhysicalStoreFieldType.Decimal, 28, 2)]
        [FieldPosition(3)]
        [DecimalPrecisionValidator(2)]
        [DefaultFieldDecimalValue(0)]
        [TreeOrdering(1)]
        decimal SortWeight { get; set; }

        [ImmutableFieldId("aa23c18e-30ff-4433-9897-056a9405f8ad")]
        [FunctionBasedNewInstanceDefaultFieldValue("<f:function xmlns:f=\"http://www.composite.net/ns/function/1.0\" name=\"Composite.Co" +
                                                   "nstant.Integer\"><f:param name=\"Constant\" value=\"5\" /></f:function>")]
        [StoreFieldType(PhysicalStoreFieldType.Integer)]
        [LazyFunctionProviedProperty("<f:function xmlns:f=\"http://www.composite.net/ns/function/1.0\" name=\"Composite.Ut" +
                                     "ils.Validation.Int32NotNullValidation\" />")]
        [FieldPosition(4)]
        [IntegerRangeValidator(-2147483648, 2147483647)]
        [DefaultFieldIntValue(0)]
        [FormRenderingProfile(Label = "Max Items When Collapsed")]
        int MaxCollapsedValueCount { get; set; }

        [ImmutableFieldId("9ef9bccb-a218-4728-b14e-d62225b2a55e")]
        [FormRenderingProfile(Label = "Max Items When Expanded")]
        [StoreFieldType(PhysicalStoreFieldType.Integer, IsNullable = true)]
        [FieldPosition(5)]
        [NullIntegerRangeValidator(-2147483648, 2147483647)]
        Nullable<int> MaxExpendedValueCount { get; set; }

        [ImmutableFieldId("db81a3cb-f934-480e-a512-976168d43baf")]
        [FormRenderingProfile(Label = "Depends On", WidgetFunctionMarkup = @"<f:widgetfunction xmlns:f=""http://www.composite.net/ns/function/1.0"" name=""Composite.Widgets.String.DataIdMultiSelector""><f:param name=""OptionsType"" value=""Orckestra.Composer.CompositeC1.DataTypes.Facets.IFacet"" /><f:param name=""CompactMode"" value=""True"" /></f:widgetfunction>")]
        [StoreFieldType(PhysicalStoreFieldType.LargeString, IsNullable = true)]
        [FieldPosition(6)]
        string DependsOn { get; set; }

        [ImmutableFieldId("2cf211cd-c154-4824-84c6-f2a321c379ea")]
        [StoreFieldType(PhysicalStoreFieldType.String, 64, IsNullable = true)]
        [FieldPosition(7)]
        [NullStringLengthValidator(0, 64)]
        [FormRenderingProfile(Label = "Start Value")]
        string StartValue { get; set; }

        [ImmutableFieldId("2ad74e60-f1be-4a30-b0ea-6a3c2f51bbbf")]
        [StoreFieldType(PhysicalStoreFieldType.String, 64, IsNullable = true)]
        [FieldPosition(8)]
        [NullStringLengthValidator(0, 64)]
        [FormRenderingProfile(Label = "End Value")]
        string EndValue { get; set; }

        [ImmutableFieldId("060cce6f-3d83-4dce-aafc-0c8c1f2b3463")]
        [StoreFieldType(PhysicalStoreFieldType.String, 64, IsNullable = true)]
        [FieldPosition(9)]
        [NullStringLengthValidator(0, 64)]
        [FormRenderingProfile(Label = "Gap Size")]
        string GapSize { get; set; }

        [ImmutableFieldId("ae0debff-eca2-4ed6-8567-49bd22c9234f")]
        [FunctionBasedNewInstanceDefaultFieldValue("<f:function xmlns:f=\"http://www.composite.net/ns/function/1.0\" name=\"Composite.Co" +
                                                   "nstant.Boolean\"><f:param name=\"Constant\" value=\"True\" /></f:function>")]
        [StoreFieldType(PhysicalStoreFieldType.Boolean)]
        [FieldPosition(10)]
        [DefaultFieldBoolValue(false)]
        [FormRenderingProfile(Label = "Is Displayed")]
        bool IsDisplayed { get; set; }

        [ImmutableFieldId("af3fb7b4-5808-44e0-ac79-80f18d89f9f7")]
        [FormRenderingProfile(Label = "Promoted Values", WidgetFunctionMarkup = @"<f:widgetfunction xmlns:f=""http://www.composite.net/ns/function/1.0"" name=""Composite.Widgets.String.DataIdMultiSelector""><f:param name=""OptionsType"" value=""Orckestra.Composer.CompositeC1.DataTypes.Facets.IPromotedFacetValueSetting"" /><f:param name=""CompactMode"" value=""True"" /></f:widgetfunction>")]
        [StoreFieldType(PhysicalStoreFieldType.LargeString, IsNullable = true)]
        [FieldPosition(11)]
        string PromotedValues { get; set; }
    }
}