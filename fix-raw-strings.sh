#!/bin/bash

# Fix raw string literal indentation in C# test files
# Usage: ./fix-raw-strings.sh [directory] [--dry-run]

DIR="${1:-./RxBlazorV2.GeneratorTests}"
DRY_RUN=false

if [[ "$*" == *"--dry-run"* ]]; then
    DRY_RUN=true
    echo "DRY RUN MODE"
fi

echo "Scanning directory: $DIR"
echo ""

files_changed=0
total_lines=0

# Find all .cs files
while IFS= read -r file; do
    temp_file="${file}.tmp"
    in_raw_string=false
    file_changed=false
    lines_fixed=0

    while IFS= read -r line; do
        # Detect raw string start
        if [[ "$line" =~ =[[:space:]]*\$?\$?\"\"\" ]]; then
            in_raw_string=true
            echo "$line"
            continue
        fi

        # Detect raw string end
        if [[ "$in_raw_string" == true ]] && [[ "$line" =~ ^[[:space:]]*\"\"\"\; ]]; then
            in_raw_string=false
            echo "$line"
            continue
        fi

        # Inside raw string - remove leading whitespace
        if [[ "$in_raw_string" == true ]]; then
            # Keep empty lines as-is
            if [[ -z "${line// }" ]]; then
                echo "$line"
            else
                # Remove all leading spaces/tabs
                trimmed="${line#"${line%%[![:space:]]*}"}"
                if [[ "$trimmed" != "$line" ]]; then
                    ((lines_fixed++))
                    file_changed=true
                fi
                echo "$trimmed"
            fi
            continue
        fi

        # Normal line
        echo "$line"
    done < "$file" > "$temp_file"

    if [[ "$file_changed" == true ]]; then
        ((files_changed++))
        ((total_lines += lines_fixed))
        echo "Fixed: $(basename "$file") ($lines_fixed lines)"

        if [[ "$DRY_RUN" == false ]]; then
            mv "$temp_file" "$file"
        else
            rm "$temp_file"
        fi
    else
        rm -f "$temp_file"
    fi
done < <(find "$DIR" -name "*.cs" -type f)

echo ""
echo "Summary: $files_changed files, $total_lines lines fixed"
if [[ "$DRY_RUN" == true ]]; then
    echo "Run without --dry-run to apply changes"
fi