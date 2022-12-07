PROJECT_LIST=("Core" "Core.Tests" "Fonts" "SyntaxHighlighting")

# Reset output directory
rm -r ./Packages
mkdir ./Packages
if [ "$?" != "0" ]; then
    exit
fi

# Build packages
for i in "${!PROJECT_LIST[@]}"; do
    PROJECT=${PROJECT_LIST[$i]}

    # Build
    dotnet build $PROJECT -c Release
    if [ "$?" != "0" ]; then
        exit
    fi

    # Package
    dotnet pack $PROJECT -c Release -o ./Packages
done