echo "Deleting all bin and obj folders..."
find . -iname "bin" -o -iname "obj" -o -iname "node_modules" | xargs rm -rf
echo "Your bin and obj folders deleted!"