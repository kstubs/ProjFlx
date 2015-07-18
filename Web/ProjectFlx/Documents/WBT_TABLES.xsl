<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wbt="myWebTemplater.1.0" exclude-result-prefixes="wbt">

    <!-- Table Matches -->
    <xsl:template match="results" mode="wbt:default-table">
        <xsl:param name="force-grid-view" select="false()"/>
        <xsl:param name="caption" select="@name"/>

        <h3>
            <xsl:value-of select="$caption"/>
        </h3>
        <table class="table table-bordered table-hover table-striped">
            <xsl:choose>
                <xsl:when test="$force-grid-view">
                    <tr>
                        <xsl:apply-templates select="schema/query/fields/field" mode="wbt:default-table-multirow-header"/>
                    </tr>
                    <xsl:apply-templates select="result/row" mode="wbt:default-table-multirow"/>                    
                </xsl:when>
                <xsl:when test="count(result/row) = 1">
                    <xsl:apply-templates select="result/row" mode="wbt:default-table"/>
                </xsl:when>
                <xsl:otherwise>
                    <!-- make header row -->
                    <tr>
                        <xsl:apply-templates select="schema/query/fields/field" mode="wbt:default-table-multirow-header"/>
                    </tr>
                    <!-- results for table -->
                    <xsl:apply-templates select="result/row" mode="wbt:default-table-multirow"/>
                </xsl:otherwise>
            </xsl:choose>

        </table>

        <xsl:call-template name="wbt:paging"/>
    </xsl:template>


    <xsl:template match="row" mode="wbt:default-table">
        <xsl:apply-templates select="ancestor::results[1]/schema/query/fields/field" mode="wbt:default-table">
            <xsl:with-param name="current" select="."/>
        </xsl:apply-templates>
    </xsl:template>

    <xsl:template match="field" mode="wbt:default-table">
        <xsl:param name="current" select="/"/>
        <xsl:param name="field_name" select="@name"/>

        <tr>
            <td>
                <xsl:value-of select="$field_name"/>
            </td>
            <td>
                <xsl:value-of select="$current/@*[name()=$field_name]"/>
            </td>
        </tr>
    </xsl:template>

    <xsl:template match="field[@GridView='false' or @ForView='false']" mode="wbt:default-table-multirow" priority="2"/>
    <xsl:template match="field[@GridView='false' or @ForView='false']" mode="wbt:default-table-multirow-header" priority="2"/>

    <xsl:template match="field" mode="wbt:default-table-multirow-header">
        <th>
            <xsl:choose>
                <xsl:when test="@GridViewDisplay">
                    <xsl:value-of select="@GridViewDisplay"/>
                </xsl:when>
                <xsl:when test="@display">
                    <xsl:value-of select="@display"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="@name"/>
                </xsl:otherwise>
            </xsl:choose>
        </th>
    </xsl:template>

    <xsl:template match="row" mode="wbt:default-table-multirow">
        <tr>
            <xsl:apply-templates select="ancestor::results[1]/schema/query/fields/field" mode="wbt:default-table-multirow">
                <xsl:with-param name="current" select="."/>
            </xsl:apply-templates>
        </tr>
    </xsl:template>

    <xsl:template match="field" mode="wbt:default-table-multirow">
        <xsl:param name="current" select="/"/>
        <xsl:param name="field_name" select="@name"/>

        <td>
            <xsl:value-of select="$current/@*[name()=$field_name]"/>
        </td>
    </xsl:template>

    <!-- Query Vars -->
    <xsl:template match="BROWSER" mode="wbt:default-table">
        <h3>BROWSER VARS</h3>
        <table class="table table-borders table-hover">
            <!-- detail -->
            <xsl:apply-templates mode="wbt:default-table"/>
        </table>
    </xsl:template>

    <xsl:template match="FORMVARS | QUERYVARS | COOKIEVARS | SESSIONVARS" mode="wbt:default-table">
        <tr>
            <th colspan="2">
                <xsl:value-of select="name()"/>
            </th>
        </tr>
        <xsl:apply-templates select="ELEMENT" mode="wbt:default-table"/>
    </xsl:template>

    <xsl:template match="ELEMENT" mode="wbt:default-table">
        <tr>
            <td>
                <xsl:value-of select="@name"/>
            </td>
            <td>
                <xsl:value-of select="."/>
            </td>
        </tr>
    </xsl:template>

    <!-- tags -->
    <xsl:template match="TAGS" mode="wbt:default-table">
        <h3>TAGS</h3>
        <table class="table table-borders table-hover">
            <!-- detail -->
            <xsl:apply-templates mode="wbt:default-table"/>
        </table>
    </xsl:template>

    <xsl:template match="TAG" mode="wbt:default-table">
        <tr>
            <td>
                <xsl:value-of select="@*[1]"/>
            </td>
            <td>
                <xsl:value-of select="."/>
            </td>
        </tr>
    </xsl:template>



</xsl:stylesheet>
