#!/bin/bash

# CallistraAgent Database Setup Script (Bash)
# This script creates the database using sqlcmd or provides alternative instructions

echo "=============================================================================="
echo "CallistraAgent Database Setup"
echo "=============================================================================="
echo ""

# Check if sqlcmd is available
if command -v sqlcmd &> /dev/null; then
    echo "✓ sqlcmd found"
    echo ""

    # Prompt for server name
    read -p "Enter SQL Server instance name (default: localhost): " SERVER_NAME
    SERVER_NAME=${SERVER_NAME:-localhost}

    # Prompt for authentication method
    echo ""
    echo "Select authentication method:"
    echo "1. Windows Authentication (Integrated Security)"
    echo "2. SQL Server Authentication"
    read -p "Enter choice (1 or 2): " AUTH_CHOICE

    echo ""
    echo "Creating database..."

    if [ "$AUTH_CHOICE" == "2" ]; then
        read -p "Enter SQL Server username: " USERNAME
        read -sp "Enter SQL Server password: " PASSWORD
        echo ""

        sqlcmd -S "$SERVER_NAME" -U "$USERNAME" -P "$PASSWORD" -C -i setup-database.sql
    else
        sqlcmd -S "$SERVER_NAME" -E -C -i setup-database.sql
        echo ""

        # Prompt for test data
        read -p "Do you want to insert test data? (Y/N): " INSERT_TEST_DATA
        if [ "$INSERT_TEST_DATA" == "Y" ] || [ "$INSERT_TEST_DATA" == "y" ]; then
            echo ""
            echo "Inserting test data..."

            if [ "$AUTH_CHOICE" == "2" ]; then
                sqlcmd -S "$SERVER_NAME" -U "$USERNAME" -P "$PASSWORD" -C -i insert-test-data.sql
            else
                sqlcmd -S "$SERVER_NAME" -E -C -i insert-test-data.sql
            fi

            if [ $? -eq 0 ]; then
                echo ""
                echo "✓ Test data inserted successfully!"
            fi
        fi

        echo ""
        echo "=============================================================================="
        echo "Setup Complete!"
        echo "=============================================================================="
        echo ""
        echo "Connection string for local.settings.json:"
        if [ "$AUTH_CHOICE" == "2" ]; then
            echo "Server=$SERVER_NAME;Database=CallistraAgent;User ID=$USERNAME;Password=***;TrustServerCertificate=true;"
        else
            echo "Server=$SERVER_NAME;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;"
        fi
        echo ""
    else
        echo ""
        echo "✗ Error creating database. Check the error messages above."
        echo ""
    fi
else
    echo "✗ sqlcmd not found"
    echo ""
    echo "Please use one of these alternatives:"
    echo ""
    echo "Option 1: Install SQL Server Command-Line Tools"
    echo "  Linux: https://learn.microsoft.com/sql/linux/sql-server-linux-setup-tools"
    echo "  macOS: brew install sqlcmd"
    echo ""
    echo "Option 2: Use Azure Data Studio"
    echo "  1. Open Azure Data Studio and connect to your SQL Server"
    echo "  2. Open file: $(pwd)/setup-database.sql"
    echo "  3. Click Run"
    echo "  4. Optionally run: $(pwd)/insert-test-data.sql"
    echo ""
    echo "Option 3: Use Entity Framework Core Migrations"
    echo "  cd ../src/CallistraAgent.Functions"
    echo "  dotnet ef migrations add InitialCreate"
    echo "  dotnet ef database update"
    echo ""
fi

echo "For more information, see DATABASE-SETUP.md"
echo ""
