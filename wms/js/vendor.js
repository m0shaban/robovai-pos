import { Chart, registerables } from 'chart.js';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import { createIcons, icons } from 'lucide';
import Swal from 'sweetalert2';
import * as XLSX from 'xlsx';

Chart.register(...registerables);

jsPDF.API.autoTable = function (options) {
  return autoTable(this, options);
};

window.Chart = Chart;
window.Swal = Swal;
window.XLSX = XLSX;
window.jspdf = { jsPDF };
import JsBarcode from 'jsbarcode';
window.JsBarcode = JsBarcode;
window.lucide = {
  createIcons() {
    createIcons({ icons });
  },
};
