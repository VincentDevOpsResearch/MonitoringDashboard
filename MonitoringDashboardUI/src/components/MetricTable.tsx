import { useState } from 'react';
import * as React from 'react'
import {
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  TablePagination, TextField, TableSortLabel, Box
} from '@mui/material';
import { visuallyHidden } from '@mui/utils';

interface ApiMetricTableProps {
  data: any[];
}

interface Column {
  id: 'request' | 'totalRequests' | 'requestsPerHour' | 'avgRespTime' | 'minRespTime' | 'maxRespTime' | '90thRespTime' | 'errorPercentage';
  label: string;
  minWidth?: number;
  align?: 'right';
  format?: (value: number) => string;
}

const columns: Column[] = [
  { id: 'request', label: 'Request', minWidth: 100 },
  { id: 'totalRequests', label: 'Total request', minWidth: 100, align: 'right' },
  { id: 'avgRespTime', label: 'Resp. time (Avg. ms)', minWidth: 100, align: 'right' },
  { id: 'minRespTime', label: 'Min (ms)', minWidth: 100, align: 'right' },
  { id: 'maxRespTime', label: 'Max (ms)', minWidth: 100, align: 'right' },
  { id: '90thRespTime', label: '90th (ms)', minWidth: 100, align: 'right' },
  { id: 'errorPercentage', label: 'Error %', minWidth: 100, align: 'right' },
];

type Order = 'asc' | 'desc';

const ApiMetricTable: React.FC<ApiMetricTableProps> = ({ data }) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchText, setSearchText] = useState('');
  const [order, setOrder] = useState<Order>('asc');
  const [orderBy, setOrderBy] = useState<keyof any>('request');

  const handleRequestSort = (property: keyof any) => {
    const isAsc = orderBy === property && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(property);
  };

  const handleSearch = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchText(event.target.value);
  };

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(+event.target.value);
    setPage(0);
  };

  const filteredData = data.filter((row) => {
    return Object.keys(row).some((key) =>
      row[key].toString().toLowerCase().includes(searchText.toLowerCase())
    );
  });

  const sortedData = filteredData.sort((a, b) => {
    if (orderBy && order) {
      const isAsc = order === 'asc';
      return (a[orderBy] < b[orderBy] ? -1 : 1) * (isAsc ? 1 : -1);
    }
    return 0;
  });

  const getMethodStyle = (method: string) => {
    switch (method) {
      case 'GET':
        return { color: 'green' };
      case 'POST':
        return { color: 'orange' };
      case 'DELETE':
        return { color: 'red' };
      default:
        return { color: 'black' };
    }
  };

  const renderRequestCell = (value: string) => {
    const [method, ...rest] = value.split(' ');
    const restOfString = rest.join(' ');
    return (
      <span>
        <span style={getMethodStyle(method)}>{method}</span> {restOfString}
      </span>
    );
  };

  return (
    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
      <TextField
        value={searchText}
        onChange={handleSearch}
        placeholder="Search..."
        variant="outlined"
        fullWidth
        margin="normal"
      />
      <TableContainer>
        <Table stickyHeader aria-label="sticky table">
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell
                  key={column.id}
                  align={column.align}
                  sortDirection={orderBy === column.id ? order : false}
                >
                  <TableSortLabel
                    active={orderBy === column.id}
                    direction={orderBy === column.id ? order : 'asc'}
                    onClick={() => handleRequestSort(column.id)}
                  >
                    {column.label}
                    {orderBy === column.id ? (
                      <Box component="span" sx={visuallyHidden}>
                        {order === 'desc' ? 'sorted descending' : 'sorted ascending'}
                      </Box>
                    ) : null}
                  </TableSortLabel>
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedData.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage).map((row, index) => {
              return (
                <TableRow hover role="checkbox" tabIndex={-1} key={index}>
                  {columns.map((column) => {
                    const value = row[column.id];
                    return (
                      <TableCell key={column.id} align={column.align}>
                        {column.id === 'request' ? (
                          renderRequestCell(value)
                        ) : (
                          value
                        )}
                      </TableCell>
                    );
                  })}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        rowsPerPageOptions={[10, 25, 50]}
        component="div"
        count={sortedData.length}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />
    </Paper>
  );
};

export default ApiMetricTable;
