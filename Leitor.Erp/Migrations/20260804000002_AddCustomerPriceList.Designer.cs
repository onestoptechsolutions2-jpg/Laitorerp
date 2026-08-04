using System;
using Leitor.Erp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Volo.Abp.EntityFrameworkCore;

namespace Leitor.Erp.Migrations
{
    [DbContext(typeof(ErpDbContext))]
    [Migration("20260804000002_AddCustomerPriceList")]
    partial class AddCustomerPriceList
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("_Abp_DatabaseProvider", EfCoreDatabaseProvider.PostgreSql)
                .HasAnnotation("ProductVersion", "10.0.7");
        }
    }
}
