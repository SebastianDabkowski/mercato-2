# Order CSV Export Format for Logistics Partners

This document describes the CSV export format for order and shipping data, designed for integration with external logistics systems and partners.

## Overview

Sellers can export their orders to CSV files from the Orders page. The export includes all key shipping information required for logistics processing, including buyer contact details, delivery address, order items, and tracking information.

## Filters

The following filters can be applied before export:

| Filter | Description |
|--------|-------------|
| **Status** | Filter by shipment status (Pending, Paid, Processing, Shipped, Delivered, Cancelled, Refunded) |
| **From Date** | Include orders created on or after this date |
| **To Date** | Include orders created on or before this date |
| **Customer/Order #** | Search by buyer name or order number |
| **Without Tracking** | Only include orders that do not have a tracking number assigned |

## CSV Structure

The exported CSV file contains the following columns:

| Column | Description | Example |
|--------|-------------|---------|
| **Order ID** | Unique identifier (UUID) of the main order | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` |
| **Order Number** | Human-readable order reference number | `MKT-20241130-A1B2C` |
| **Sub-Order ID** | Unique identifier (UUID) of the seller's sub-order/shipment | `x9y8z7w6-v5u4-3210-fedc-ba0987654321` |
| **Created Date** | Order creation timestamp (UTC) | `2024-11-30 14:30:00` |
| **Status** | Current shipment status | `Paid`, `Processing`, `Shipped`, etc. |
| **Buyer Name** | Full name of the buyer | `John Smith` |
| **Buyer Email** | Buyer's email address | `john.smith@example.com` |
| **Buyer Phone** | Buyer's phone number | `+1 555-123-4567` |
| **Delivery Address** | Full formatted delivery address | `123 Main St, Apt 4B, New York, NY, 10001, USA` |
| **Street** | Street address line 1 | `123 Main St` |
| **Street 2** | Street address line 2 (apartment, suite, etc.) | `Apt 4B` |
| **City** | City name | `New York` |
| **State** | State/Province/Region | `NY` |
| **Postal Code** | Postal/ZIP code | `10001` |
| **Country** | Country name | `USA` |
| **Total Amount** | Total order amount (decimal, 2 places) | `149.99` |
| **Currency** | Currency code (ISO 4217) | `USD` |
| **Shipping Method** | Selected shipping method name | `Standard Shipping` |
| **Item Count** | Number of items in this sub-order | `3` |
| **Order Items** | Summary of products ordered | `Product A (x2); Product B (x1)` |
| **Tracking Number** | Shipment tracking number (if assigned) | `1Z999AA10123456784` |
| **Carrier** | Carrier/courier name (if assigned) | `UPS` |
| **Shipped Date** | When the order was shipped (if shipped) | `2024-12-01 09:15:00` |
| **Delivered Date** | When the order was delivered (if delivered) | `2024-12-03 14:22:00` |

## File Format Details

- **Encoding**: UTF-8
- **Delimiter**: Comma (`,`)
- **Text Qualifier**: Double quotes (`"`) for values containing commas, quotes, or newlines
- **Date Format**: `YYYY-MM-DD HH:MM:SS` (UTC)
- **Decimal Format**: Period as decimal separator (e.g., `99.99`)
- **Newline**: Standard line endings

## Excel Compatibility

The CSV file is designed to open correctly in Microsoft Excel and other common spreadsheet tools. The first row contains clear column headers.

## Empty Values

- Empty string values are represented as blank (no value between delimiters)
- Optional fields like `Street 2`, `State`, `Tracking Number`, `Carrier`, `Shipped Date`, and `Delivered Date` may be empty

## Example

```csv
Order ID,Order Number,Sub-Order ID,Created Date,Status,Buyer Name,Buyer Email,Buyer Phone,Delivery Address,Street,Street 2,City,State,Postal Code,Country,Total Amount,Currency,Shipping Method,Item Count,Order Items,Tracking Number,Carrier,Shipped Date,Delivered Date
a1b2c3d4-e5f6-7890-abcd-ef1234567890,MKT-20241130-A1B2C,x9y8z7w6-v5u4-3210-fedc-ba0987654321,2024-11-30 14:30:00,Shipped,John Smith,john.smith@example.com,+1 555-123-4567,"123 Main St, Apt 4B, New York, NY, 10001, USA",123 Main St,Apt 4B,New York,NY,10001,USA,149.99,USD,Standard Shipping,2,Widget Pro (x1); Gadget Plus (x1),1Z999AA10123456784,UPS,2024-12-01 09:15:00,
```

## Volume Limits

- For large exports, consider using date range filters to limit the number of records
- The export processes all matching orders in a single batch

## Integration Notes

1. **Idempotency**: Use the `Sub-Order ID` as a unique identifier when importing into logistics systems
2. **Status Tracking**: The `Status` field indicates the current fulfillment state
3. **Address Parsing**: Individual address components (Street, City, etc.) are provided for systems that require separate fields
4. **Item Details**: The `Order Items` column provides a quick reference; for detailed item data, use the API

## Related Features

- **Excel Export**: The same data can be exported in XLSX format for enhanced Excel compatibility
- **Filter Persistence**: Applied filters are preserved during export
