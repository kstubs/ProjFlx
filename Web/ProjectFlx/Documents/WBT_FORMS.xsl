<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:wbt="myWebTemplater.1.0" exclude-result-prefixes="wbt">

    <!-- EXTERNAL SUPPORT DOCUMENTS AND SOURCE -->
    <xsl:variable name="wbt:dbxmSTATES" select="document('dbXML_states.xml')/states"/>
        
    <!-- EXTENDED FORM OBJECT -->
    <xsl:template name="wbt:DD_STATES">
        <xsl:param name="title"/>
        <xsl:param name="id"/>
        <xsl:param name="input_value"/>
        <xsl:param name="input_name">state</xsl:param>
        <xsl:param name="onchange"/>
        <xsl:param name="class"/>
        <select name="{$input_name}">
            <!-- title -->
            <xsl:if test="string($title)">
                <xsl:attribute name="title">
                    <xsl:value-of select="$title"/>
                </xsl:attribute>
            </xsl:if>
            <!-- add id -->
            <xsl:if test="string($id)">
                <xsl:attribute name="id">
                    <xsl:value-of select="$id"/>
                </xsl:attribute>
            </xsl:if>
            <!-- add onclick event if true -->
            <xsl:if test="string($onchange)">
                <xsl:attribute name="onchange">
                    <xsl:value-of select="$onchange"/>
                </xsl:attribute>
            </xsl:if>
            <!-- add class if true -->
            <xsl:if test="string($class)">
                <xsl:attribute name="class">
                    <xsl:value-of select="$class"/>
                </xsl:attribute>
            </xsl:if>
            <option value="">
                <xsl:value-of select="$title"/>
            </option>
            <xsl:for-each select="$wbt:dbxmSTATES//state">
                <option value="{@value}">
                    <xsl:if test="$input_value=@value or $input_value=@text">
                        <xsl:attribute name="selected">selected</xsl:attribute>
                    </xsl:if>
                    <xsl:value-of select="@text"/>
                </option>
            </xsl:for-each>
        </select>
    </xsl:template>
        
    <xsl:template match="parameter[@ForUpdate='false']" mode="wbt:edits" priority="1"/>
    <xsl:template match="parameter" mode="wbt:edits">
        <xsl:param name="data-row"/>
        <xsl:variable name="required" select="contains(@regx, 'required')"/>
        <xsl:variable name="email" select="contains(@regx, 'email')"/>
        
        <!-- lookup previous value -->
        <xsl:variable name="param" select="."/>
        <div class="form-group">
            <label for="{@name}" class="col-sm-3">
                <xsl:value-of select="@display"/>
            </label>
            <div class="col-sm-9">
                <div>
                    <xsl:if test="$required or $email">
                        <xsl:attribute name="class">input-group</xsl:attribute>
                    </xsl:if>
                    <xsl:if test="$email">
                        <div class="input-group-addon">
                            <span>@</span>
                        </div>
                    </xsl:if>
                    <xsl:choose>
                        <xsl:when test="@display_type='select-states'">
                            <xsl:call-template name="wbt:DD_STATES">
                                <xsl:with-param name="class">form-control</xsl:with-param>
                                <xsl:with-param name="input_value" select="."/>
                            </xsl:call-template>
                        </xsl:when>
                        <xsl:otherwise>
                            <input class="form-control" type="text" name="{@name}" id="{@name}_id">
                                <xsl:if test="@display-type='email'">
                                    <xsl:attribute name="type">email</xsl:attribute>
                                </xsl:if>
                                <xsl:attribute name="value">
                                    <xsl:value-of select="$data-row/@*[name(.)=$param/@lookup_value]"/>
                                </xsl:attribute>
                                <xsl:if test="string(@size)">
                                    <xsl:attribute name="data-max-len">
                                        <xsl:value-of select="@size"/>
                                    </xsl:attribute>
                                </xsl:if>
                                <xsl:if test="@Locked='true'">
                                    <xsl:attribute name="readonly">readonly</xsl:attribute>
                                </xsl:if>
                                <xsl:if test="@regx">
                                    <xsl:attribute name="class">
                                        <xsl:text>form-control </xsl:text>
                                        <xsl:value-of select="@regx"/>
                                    </xsl:attribute>
                                </xsl:if>
                            </input>
                        </xsl:otherwise>
                    </xsl:choose>
                    <xsl:if test="$required">
                        <div class="input-group-addon required">
                            <i class="fa fa-exclamation-circle" title="This is a required field"/>
                        </div>
                    </xsl:if>
                </div>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="field" mode="wbt:edits">
        <xsl:param name="current" select="/.."/>
        <xsl:param name="field_name" select="@name"/>
        <xsl:param name="update_query" select="/.."/>
        <xsl:variable name="mapped-field" select="$update_query/parameter[@lookup_value=$field_name]"/>

        <!-- only build inputs for mapped-field(s) -->
        <xsl:if test="$mapped-field">
            <xsl:variable name="display">
                <xsl:choose>
                    <xsl:when test="$mapped-field/@display">
                        <xsl:value-of select="$mapped-field/@display"/>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:value-of select="$mapped-field/@name"/>
                    </xsl:otherwise>
                </xsl:choose>                
            </xsl:variable>

            <div class="form-group">
                <xsl:if test="$mapped-field/@ForView='False'">
                    <xsl:attribute name="style">display:none</xsl:attribute>
                </xsl:if>
                <label for="{$field_name}" class="col-sm-4 control-label"><xsl:value-of select="$display"/>
                    <xsl:if test="contains($mapped-field/@regx, 'required')">
                        <i class="fa fa-exclamation-circle"/>
                    </xsl:if>
                </label>
                <div class="col-sm-9">
                    <input type="text" name="{$field_name}" value="{$current/@*[name()=$field_name]}" class="form-control {$mapped-field/@regx}">
                        <xsl:if test="$mapped-field/@ForUpdate='False'">
                            <xsl:attribute name="readonly">readonly</xsl:attribute>
                        </xsl:if>
                    </input>
                </div>
            </div>
        </xsl:if>
    </xsl:template>

    <xsl:template match="results | result | row" mode="wbt:edits">
        <xsl:param name="project"/>
        <xsl:param name="query"/>
        <xsl:apply-templates select="/flx/app/ProjectSql[@name=$project and @query=$query]/*/parameters/parameter" mode="wbt:edits">
            <xsl:with-param name="data-row" select="descendant-or-self::row"/>
            <xsl:sort select="descendant-or-self::row/@display_order" case-order="lower-first" data-type="number"/>
        </xsl:apply-templates>
    </xsl:template>
    
    <xsl:template match="input[@type='text' or @type='hidden']" mode="identity-translate">
        <xsl:copy>
            <xsl:apply-templates select="@*[name()!='value']" mode="identity-translate"/>
            <xsl:attribute name="value">
                <xsl:value-of select="@value | key('wbt:key_FormVars', @name)"/>
            </xsl:attribute>            
        </xsl:copy>
    </xsl:template>
    
    <xsl:template match="input[@type='text' and @name='state']" mode="identity-translate">
        <xsl:call-template name="wbt:DD_STATES">
            <xsl:with-param name="input_value" select="@value" />
        </xsl:call-template>
    </xsl:template>
    
</xsl:stylesheet>