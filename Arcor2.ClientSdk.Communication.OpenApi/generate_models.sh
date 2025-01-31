#!/bin/bash

INPUT_FILE=""
OUTPUT_DIR="./"
MODELS_DIR="Models"
PACKAGE_NAME="Arcor2.ClientSdk.Communication.OpenApi"
OPENAPI_JAR="openapi-generator-cli.jar"
TEMP_DIR="./ARCOR_MODELGEN_SCRIPT_TEMP_DIR"

print_usage() {
    echo "Usage: $0 -i <input_file> [-o <output_dir>] [-p <package_name>] [-j <openapi_jar_path>]"
    echo ""
    echo "Options:"
    echo "  -i  Input OpenAPI specification file (required)"
    echo "  -o  Output directory (default: ./output)"
    echo "  -p  Package name (default: Arcor2.ClientSdk.Communication.OpenApi)"
    echo "  -j  Path to OpenAPI generator JAR (default: openapi-generator-cli.jar)"
    echo "  -h  Show this help message"
}

while getopts "i:o:p:j:h" opt; do
    case $opt in
        i) INPUT_FILE="$OPTARG";;
        o) OUTPUT_DIR="$OPTARG";;
        p) PACKAGE_NAME="$OPTARG";;
        j) OPENAPI_JAR="$OPTARG";;
        h) print_usage; exit 0;;
        ?) print_usage; exit 1;;
    esac
done

if [ -z "$INPUT_FILE" ]; then
    echo "Error: Input file is required"
    print_usage
    exit 1
fi

if [ ! -f "$INPUT_FILE" ]; then
    echo "Error: Input file '$INPUT_FILE' not found"
    exit 1
fi

if [ ! -f "$OPENAPI_JAR" ]; then
    echo "Error: OpenAPI generator JAR '$OPENAPI_JAR' not found"
    exit 1
fi

mkdir -p "$TEMP_DIR"

echo "Generating models from $INPUT_FILE..."

java -jar "$OPENAPI_JAR" generate \
    -i "$INPUT_FILE" \
    -g csharp \
    -o "$TEMP_DIR" \
    --global-property models,modelDocs=false,supportingFiles=false \
    --additional-properties targetFramework=netstandard2.1,packageName="$PACKAGE_NAME",equatable=true,nullableReferenceTypes=true,sourceFolder="$MODELS_DIR"

if [ $? -ne 0 ]; then
    echo "Error: Model generation failed"
    exit 1
fi

mkdir -p "$OUTPUT_DIR/$MODELS_DIR/"
mv "$TEMP_DIR/$MODELS_DIR/$PACKAGE_NAME/Model/"* "$OUTPUT_DIR/$MODELS_DIR/"
rm -fr "$TEMP_DIR"

# Change the emition of default values for these [File]="properties" pairs
declare -A modificate_defvalue_emmision=(
    ["SceneChanged.cs"]="data"
    ["ProjectChanged.cs"]="data"
    ["ObjectModel.cs"]="box cylinder mesh sphere type"
    ["InverseKinematicsRequestArgs.cs"]="arm_id"
    ["InverseKinematicsRequest.cs"]="data"
    ["ObjectTypeMeta.cs"]="object_model needs_parent_type settings abstract disabled problem"
    ["AddLogicItemRequestArgs.cs"]="condition"
    ["AddObjectToSceneRequestArgs.cs"]="pose settings"
    ["MoveToActionPointRequestArgs.cs"]="end_effector_id orientation_id joints_id arm_id"
    ["UpdateActionPointJointsRequestArgs.cs"]="joints"
    ["CopyActionPointRequestArgs.cs"]="position"
    ["StepRobotEefRequestArgs.cs"]="pose arm_id"
    ["RobotArg.cs"]="arm_id"
    ["UpdateActionPointUsingRobotRequestArgs.cs"]="arm_id"
    ["AddApUsingRobotRequestArgs.cs"]="arm_id"
    ["AddActionPointJointsUsingRobotRequestArgs.cs"]="arm_id"
    ["MoveToPoseRequestArgs.cs"]="arm_id"
    ["GetEndEffectorPoseRequestArgs.cs"]="arm_id"
    ["GetEndEffectorsRequestArgs.cs"]="arm_id"
    ["GetEndEffectorsRequest.cs"]="data"
    ["ForwardKinematicsRequestArgs.cs"]="arm_id"
    ["HandTeachingModeRequestArgs.cs"]="arm_id"
    ["SetEefPerpendicularToWorldRequestArgs.cs"]="arm_id"
    ["ActionStateBeforeData.cs"]="action_id"
    ["ActionStateAfterData.cs"]="action_id"
)

echo "Applying model modifications..."
for file in "${!modificate_defvalue_emmision[@]}"; do
    file_path="$OUTPUT_DIR/$MODELS_DIR/$file"
    if [ -f "$file_path" ]; then
        # First attempt: modify properties without IsRequired
        for prop in ${modificate_defvalue_emmision[$file]}; do
            sed -i'' "s/DataMember(Name = \"$prop\", EmitDefaultValue=true)/DataMember(Name=\"$prop\", EmitDefaultValue = false)/" "$file_path"
        done

        # Second attempt: modify properties with IsRequired = true
        for prop in ${modificate_defvalue_emmision[$file]}; do
            sed -i'' "s/DataMember(Name = \"$prop\", IsRequired = true, EmitDefaultValue = true)/DataMember(Name = \"$prop\", IsRequired = true, EmitDefaultValue = false)/" "$file_path"
        done

        echo "Modified $file"
    else
        echo "Warning: File $file not found"
    fi
done

echo "Clearing and applying namespace modifications..."

for file in "$OUTPUT_DIR/$MODELS_DIR"/*; do
    if [ -f "$file" ]; then
        # Update namespace
        sed -i'' "s/namespace Arcor2.ClientSdk.Communication.OpenApi.Model/namespace Arcor2.ClientSdk.Communication.OpenApi.Models/" "$file"

        # Remove OpenAPIDateConverter using statement
        sed -i'' "/using OpenAPIDateConverter = Arcor2.ClientSdk.Communication.OpenApi.Client.OpenAPIDateConverter;/d" "$file"

        echo "Modified $(basename "$file")"
    fi
done

echo "Model generation complete. Output directory: $OUTPUT_DIR/$MODELS_DIR"
