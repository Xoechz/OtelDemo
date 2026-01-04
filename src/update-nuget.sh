#!/usr/bin/env bash
regex='PackageReference Include="([^"]*)" Version="([^"]*)"'
find . -name "*.*proj" | while read proj
do
  while read line
  do
    if [[ $line =~ $regex ]]
    then
        name=${BASH_REMATCH[1]}
        version=${BASH_REMATCH[2]}
        echo "Updating $name version $version in project $proj to latest"
        dotnet add $proj package $name
    fi
  done < $proj
done